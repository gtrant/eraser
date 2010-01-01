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
			header('location: ' . $this->Link);
		else if (substr($this->Link, 0, 1) == '?')
			Download::DownloadFile(substr($this->Link, 1));
		else
			throw new Exception('Unknown download link');
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
	
		echo file_get_contents($downloadFolder . $path);
	}
};

class Build extends Download
{
	public function Build($path, $revision)
	{
		$query = mysql_query(sprintf('SELECT downloads.DownloadID FROM downloads INNER JOIN builds ON
			downloads.DownloadID=builds.DownloadID WHERE
				builds.Path=\'%s\' AND builds.Revision=%d',
			mysql_real_escape_string($path), intval($revision)));
		
		//See if the build already has a database entry
		if (($row = mysql_fetch_row($query)) === false || !$row[0])
		{
			$this->ID = Build::InsertBuild($path, $revision);
		}
		else
		{
			$this->ID = intval($row[0]);

			//Check that the folder has not been removed. This may indicate supersedence.
			if (!file_exists(Build::GetPath($this->Path, $this->Revision)))
				$this->Superseded = 1;
		}
	}
	
	public static function Get()
	{
		$result = array();
		$builds = array('Eraser5' => 'Eraser 5', 'Eraser6' => 'Eraser 6', 'Eraser6.2' => 'Eraser 6.2');
		foreach ($builds as $path => $buildName)
		{
			$revisions = opendir(Build::GetPath($path));
			$result[$buildName] = array();

			while (($revision = readdir($revisions)) !== false)
			{
				if (!sprintf('downloads/builds/%s/%s', $path, $revision) || $revision == '.' || $revision == '..')
					continue;
				
				try
				{
					$result[$buildName][] = new Build($path, intval(substr($revision, 1)));
				}
				catch (Exception $e)
				{
				}
			}
		}
		
		return $result;
	}
	
	public static function GetBuildFromID($downloadID)
	{
		$query = mysql_query(sprintf('SELECT * FROM builds WHERE DownloadID=%d', intval($downloadID)));
		if (($row = mysql_fetch_array($query)) === false || !$row[0])
			return null;

		return intval($row[0]) ? new Build($row['Path'], $row['Revision']) : null;
	}
	
	public function __get($varName)
	{
		$sql = sprintf('SELECT %%s FROM builds WHERE DownloadID=%d', $this->ID);
		switch ($varName)
		{
			case 'Revision':
			case 'Path':
				$sql = sprintf($sql, $varName);
				break;

			default:
				return parent::__get($varName);
		}
		
		$query = mysql_query($sql);
		$row = $query ? mysql_fetch_row($query) : null;
		
		return $row ? $row[0] : null;
	}
	
	private static function InsertBuild($path, $revision)
	{
		//It doesn't. Find the binary that users will get to download.
		$directory = opendir(Build::GetPath($path, $revision));
		$installer = null;
		$installerFilesize = 0;
		while (($file = readdir($directory)) !== false)
		{
			$filePath = Build::GetPath($path, $revision) . '/' . $file;
			if (is_file($filePath))
			{
				$pathInfo = pathinfo($filePath);
				if ($pathInfo['extension'] == 'exe')
				{
					$installer = sprintf('builds/%s/r%s/%s', $path, $revision, $file);
					$installerFilesize = filesize($filePath);
					break;
				}
			}
		}
		
		if (empty($installer) || $installerFilesize == 0)
		{
			//It is a build in progress, don't create anything.
			throw new Exception(sprintf('Build %s r%d is incomplete.', $path, $revision));
		}
		
		//Insert the build into the database.
		mysql_query('START TRANSACTION');
		mysql_query(sprintf('INSERT INTO downloads (Name, Released, `Type`, Version, PublisherID, Architecture, Filesize, Link)
				VALUES (
					\'%1$s r%2$d\', \'%4$s\' , \'build\', \'r%2$d\', 1, \'any\', %3$d, \'?%5$s\'
				)',
			mysql_real_escape_string($path), intval($revision), $installerFilesize,
			PhpToMySqlTimestamp(filemtime(Build::GetPath($path, $revision))),
			mysql_real_escape_string($installer)))
				or die(mysql_error());
		mysql_query(sprintf('INSERT INTO builds (DownloadID, Path, Revision)
				VALUES (
					LAST_INSERT_ID(), \'%1s\', %2$d
				)',
			mysql_real_escape_string($path), intval($revision))) or die(mysql_error());

		if (!mysql_affected_rows())
			throw new Exception(sprintf('Could not create new build %s r%d. MySQL Error: %s', $path, $revision, mysql_error()));
		$buildId = mysql_insert_id();
		
		mysql_query('COMMIT');
		
		//Ensure that only 3 builds are not superseded at any one time.
		mysql_query('START TRANSACTION');
		mysql_query(sprintf('UPDATE downloads SET Superseded=1
			WHERE Name LIKE \'%s%%\' AND Superseded=0', $path));
		mysql_query(sprintf('UPDATE downloads SET Superseded=0
			WHERE Name LIKE \'%s%%\'
			ORDER BY DownloadID DESC
			LIMIT 3', $path));
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
