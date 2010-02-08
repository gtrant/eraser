<?php
require('database.php');

class Download
{
	protected $ID;

	public function Download($downloadID)
	{
		$query = mysql_query(sprintf('SELECT COUNT(DownloadID) FROM downloads WHERE DownloadID=%d',
			intval($downloadID)));
		if (($row = mysql_fetch_row($query)) === false || $row[0] == 0)
			throw new Exception(sprintf('Could not find download %d', $downloadID));

		$this->ID = $downloadID;
	}
	
	public function InitiateDownload()
	{
		//Register the download
		mysql_query(sprintf('INSERT INTO download_log (DownloadID) VALUES (%d)', $this->ID));
		
		if (preg_match('/http(s{0,1}):\/\/(.*)/', $this->Link))
		{
			header('location: ' . $this->Link);
		}
		else if (substr($this->Link, 0, 1) == '?')
		{
			//Get a name to call the file.
			$pathInfo = pathinfo(substr($this->Link, 1));
			Download::DownloadFile(substr($this->Link, 1),
				sprintf('%s.%s', $this->Name, $pathInfo['extension']));
		}
		else
		{
			throw new Exception('Unknown download link');
		}
	}
	
	public function __get($varName)
	{
		$sql = sprintf('SELECT %%s FROM downloads WHERE DownloadID=%d', $this->ID);
		switch ($varName)
		{
			case 'ID':
				return $this->ID;

			case 'Downloads':
				$sql = sprintf('SELECT Downloads FROM download_statistics WHERE DownloadID=%d', $this->ID);
				break;

			case 'Name':
			case 'Released':
			case 'Superseded':
			case 'Link':
				$sql = sprintf($sql, $varName);
				break;
		}
		
		if (empty($sql))
			return null;
		
		$query = mysql_query($sql);
		$row = $query ? mysql_fetch_row($query) : null;
		$result = $row ? $row[0] : null;
		
		if ($result !== null)
			switch ($varName)
			{
				case 'Downloads':
					$result = intval($result);
					break;
				case 'Released':
					$result = MySqlToPhpTimestamp($result);
					break;
			}
		
		return $result;
	}

	public function __set($varName, $value)
	{
		$sql = sprintf('UPDATE downloads SET %%s=%%s WHERE DownloadID=%d', $this->ID);
		switch ($varName)
		{
			case 'Superseded':
				$sql = sprintf($sql, $varName, intval($value));
				break;
			default:
				return;
		}
		
		if (empty($sql))
			return;
		mysql_query($sql);
	}
	
	private static function DownloadFile($path, $visibleName = null)
	{
		$downloadFolder = dirname(__FILE__) . '/../downloads/';
		$visibleName = $visibleName == null ? basename($path) : $visibleName;

		header('Content-Type: application/octet-stream');
		header('Content-Length: ' . filesize($downloadFolder . $path));
		if (strpos($_SERVER['HTTP_USER_AGENT'], 'MSIE') !== false)
		{
			//IE browser
			header('Content-Disposition: inline; filename="' . $visibleName . '"');
			header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
			header('Pragma: public');
		}
		else
		{
			header('Content-Disposition: attachment; filename="' . $visibleName . '"');
			header('Pragma: no-cache');
		}

		readfile($downloadFolder . $path);
	}
};

class Build extends Download
{
	public function Build($branch, $revision)
	{
		$query = mysql_query(sprintf('SELECT DownloadID FROM builds WHERE
				Branch=\'%s\' AND Revision=%d',
			mysql_real_escape_string($branch), intval($revision)));
		
		//See if the build already has a database entry
		if (($row = mysql_fetch_row($query)) === false || !$row[0])
		{
			throw new Exception('Build does not exist');
		}
		else
		{
			$this->ID = intval($row[0]);

			//Check that the folder has not been removed. This may indicate supersedence.
			$downloadFolder = dirname(__FILE__) . '/../downloads/';
			if (!file_exists($downloadFolder . substr($this->Link, 1)))
				$this->Superseded = 1;
		}
	}
	
	public static function Get()
	{
		$result = array();
		$builds = array('Eraser5' => 'Eraser 5', 'Eraser6' => 'Eraser 6.0', 'Eraser6.2' => 'Eraser 6.2');
		$versions = array('Eraser5' => '5.8.9', 'Eraser6' => '6.0.6', 'Eraser6.2' => '6.1.0');
		foreach ($builds as $branchName => $buildName)
		{
			$revisions = opendir(Build::GetPath($branchName));
			$result[$buildName] = array();

			while (($revision = readdir($revisions)) !== false)
			{
				if ($revision == '.' || $revision == '..')
					continue;
				
				$pathInfo = pathinfo($revision);
				$revisionID = intval(substr($pathInfo['filename'], 1));
				if (Build::BuildExists($branchName, $revisionID))
				{
					$result[$buildName][] = new Build($branchName, $revisionID);
				}
				else
				{
					$result[$buildName][] = Build::GetBuildFromID(
						Build::InsertBuild($branchName, $versions[$branchName], $revisionID,
							Build::GetPath($branchName) . '/' . $revision));
				}
			}
		}
		
		return $result;
	}
	
	public static function BuildExists($branch, $revision)
	{
		$query = mysql_query(sprintf('SELECT DownloadID FROM builds WHERE
				Branch=\'%s\' AND Revision=%d',
			mysql_real_escape_string($branch), intval($revision)));
		return mysql_num_rows($query) == 1;
	}
	
	public static function GetBuildFromID($downloadID)
	{
		$query = mysql_query(sprintf('SELECT * FROM builds WHERE DownloadID=%d', intval($downloadID)));
		if (($row = mysql_fetch_array($query)) === false || !$row[0])
			return null;

		return intval($row[0]) ? new Build($row['Branch'], $row['Revision']) : null;
	}
	
	public function __get($varName)
	{
		$sql = sprintf('SELECT %%s FROM builds WHERE DownloadID=%d', $this->ID);
		switch ($varName)
		{
			case 'Revision':
			case 'Branch':
				$sql = sprintf($sql, $varName);
				break;

			default:
				return parent::__get($varName);
		}
		
		$query = mysql_query($sql);
		$row = $query ? mysql_fetch_row($query) : null;
		
		return $row ? $row[0] : null;
	}
	
	private static function InsertBuild($branch, $version, $revision, $buildPath)
	{
		//Find the binary that users will get to download.
		$installerPath = null;
		$installerSize = 0;

		//If $buildPath is a directory, it contains the installer.
		if (is_dir($buildPath))
		{
			$directory = opendir($buildPath);
			
			while (($file = readdir($directory)) !== false)
			{
				$filePath = $buildPath . '/' . $file;
				if (is_file($filePath))
				{
					$pathInfo = pathinfo($filePath);
					if ($pathInfo['extension'] == 'exe')
					{
						$installerPath = sprintf('builds/%s/r%s/%s', $branch, $revision, $file);
						$installerSize = filesize($filePath);
						break;
					}
				}
			}
		}
		//If $buildPath.exe is a file, it's the installer we want.
		else if (is_file($buildPath))
		{
			$installerPath = sprintf('builds/%s/%s', $branch, basename($buildPath));
			$installerSize = filesize($buildPath);
		}

		if (empty($installerPath))
		{
			//It is a build in progress, don't create anything.
			throw new Exception(sprintf('Build %s r%d is incomplete.', $branch, $revision));
		}
		
		//Insert the build into the database.
		mysql_query('START TRANSACTION');
		mysql_query(sprintf('INSERT INTO downloads (Name, Released, `Type`, Version, PublisherID, Architecture, Filesize, Link)
				VALUES (
					\'%1$s %2$s.%3$d\', \'%5$s\' , \'build\', \'%2$s.%3$d\', 1, \'any\', %4$d, \'?%6$s\'
				)',
			mysql_real_escape_string($branch), mysql_real_escape_string($version), intval($revision),
			$installerSize, PhpToMySqlTimestamp(filemtime($buildPath)),
			mysql_real_escape_string($installerPath)))
				or die(mysql_error());
		mysql_query(sprintf('INSERT INTO builds (DownloadID, Branch, Revision)
				VALUES (
					LAST_INSERT_ID(), \'%s\', %d
				)',
			mysql_real_escape_string($branch), intval($revision)))
				or die(mysql_error());

		if (!mysql_affected_rows())
			throw new Exception(sprintf('Could not create new build %s r%d. MySQL Error: %s', $path, $revision, mysql_error()));
		$buildId = mysql_insert_id();
		
		mysql_query('COMMIT');
		
		//Ensure that only 3 builds are not superseded at any one time.
		mysql_query('START TRANSACTION');
		mysql_query(sprintf('UPDATE downloads SET Superseded=1
			WHERE DownloadID IN (
				SELECT DownloadID FROM builds where Branch=\'%s\'
			)',
			mysql_real_escape_string($branch)));
		mysql_query(sprintf('UPDATE downloads SET Superseded=0
			WHERE DownloadID IN (
				SELECT DownloadID FROM builds where Branch=\'%s\'
			)
			ORDER BY DownloadID DESC
			LIMIT 3', mysql_real_escape_string($branch)));
		mysql_query('COMMIT');
		return $buildId;
	}
	
	private static function GetPath($path, $revision = null)
	{
		return sprintf('%s/../downloads/builds/%s%s', dirname(__FILE__), $path,
			$revision === null ? '' : sprintf('/r%s', $revision));
	}
};
?>