<?php
abstract class UpdateListBase
{
	protected abstract function GetVersion();
	protected abstract function GetDownloads($clientVersion);
	protected abstract function OutputBodyXml($clientVersion);
	
	public function Output($clientVersion)
	{
		$this->OutputHeader();
		$this->OutputBodyXml($clientVersion);
		$this->OutputFooter();
	}

	private function OutputHeader()
	{
		printf('<?xml version="1.0"?>
<updateList version="%s">
', $this->GetVersion());
	}
	
	protected function OutputDownloads($clientVersion)
	{
		$query = $this->GetDownloads($clientVersion);

		$lastItemType = null;
		while ($row = mysql_fetch_array($query))
		{
			if ($row['Type'] != $lastItemType)
			{
				if ($lastItemType !== null)
					printf('	</%s>' . "\n", $lastItemType);
				printf('	<%s>' . "\n", $row['Type']);
				$lastItemType = $row['Type'];
			}
		
			//Get the link to the download. We got three forms, relative, absolute and query.
			//Relative links are mirrored by SF.
			//Absolute links... are absolute links.
			//Query links are prefixed with ?, they are handled by download.php on the Eraser website.
			if (substr($row['Link'], 0, 1) == '?')
				$link = 'http://' . $_SERVER['SERVER_NAME'] . '/download.php?id=' . $row['DownloadID'];
			else
				$link = $row['Link'];
			printf('		<item name="%s" version="%s" publisher="%s" architecture="%s" filesize="%d">%s</item>
', htmlentities($row['Name']), $row['Version'], htmlentities($row['PublisherName']), $row['Architecture'],
				$row['Filesize'], htmlentities($link));
		}
		
		if (!empty($lastItemType))
			printf('	</%s>' . "\n", $lastItemType);
	}

	private function OutputFooter()
	{
		printf('</updateList>');
	}
}

class UpdateList1 extends UpdateListBase
{
	protected function GetVersion()
	{
		return "1.0";
	}
	
	protected function GetDownloads($clientVersion)
	{
		//Prepare the list of updates
		return mysql_query(sprintf('SELECT downloads.*, publishers.Name as PublisherName
			FROM downloads
			INNER JOIN publishers ON
				downloads.PublisherID=publishers.PublisherID
			WHERE
				(Superseded = 0) AND
				(`Type` <> \'build\') AND
				(
					(MinVersion IS NULL AND MaxVersion IS NULL) OR
					(MinVersion IS NULL AND MaxVersion > \'%1$s\') OR
					(MinVersion <= \'%1$s\' AND MaxVersion IS NULL) OR
					(MinVersion <= \'%1$s\' AND MaxVersion > \'%1$s\')
				)
			ORDER BY `Type` ASC', mysql_real_escape_string($clientVersion)));
	}
	
	private function OutputMirrors()
	{
		//Output the list of mirrors
		$query = mysql_query('SELECT * FROM mirrors ORDER By Continent, Country, City');
		echo '	<mirrors>
		<mirror location="(automatically decide)">http://downloads.sourceforge.net/eraser/</mirror>
';
		while ($row = mysql_fetch_array($query))
		{
			printf('		<mirror location="%s, %s">%s</mirror>' . "\n", $row['City'], $row['Country'],
				$row['URL']);
		}
		echo '	</mirrors>';
	}

	protected function OutputBodyXml($clientVersion)
	{
		$this->OutputMirrors();
		$this->OutputDownloads($clientVersion);
	}
}

class UpdateList1_1 extends UpdateListBase
{
	protected function GetVersion()
	{
		return "1.1";
	}
	
	protected function GetDownloads($clientVersion)
	{
		//Prepare the list of updates
		return mysql_query(sprintf('SELECT downloads.*, publishers.Name as PublisherName
			FROM downloads
			INNER JOIN publishers ON
				downloads.PublisherID=publishers.PublisherID
			WHERE
				(Superseded = 0) AND
				(
					(															-- Program Updates
						(`Type` <> \'build\') AND
						(MinVersion IS NULL AND MaxVersion IS NULL) OR
						(MinVersion IS NULL AND MaxVersion > \'%1$s\') OR
						(MinVersion <= \'%1$s\' AND MaxVersion IS NULL) OR
						(MinVersion <= \'%1$s\' AND MaxVersion > \'%1$s\')
					) OR
					(															-- Nightly builds greater than our version
						(`Type` = \'build\') AND
						(Version > \'%1$s\')
					)
				)
			ORDER BY `Type` ASC', mysql_real_escape_string($clientVersion)));
	}
	
	protected function OutputBodyXml($clientVersion)
	{
		$this->OutputDownloads($clientVersion);
	}
}
?>
