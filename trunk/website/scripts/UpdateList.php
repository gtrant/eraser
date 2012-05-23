<?php
require_once('Database.php');
require_once('Download.php');
require_once ('Build.php');

/**
 * Base class for all update lists types.
 */
abstract class UpdateListBase
{
	/**
	 * Gets the version of the update list generated.
	 *
	 * @return string
	 */
	protected abstract function GetVersion();

	/**
	 * Gets the updates that the client may require.
	 *
	 * @param string $clientVersion The version string of the client.
	 * @return array                An array of Download objects which will be presented to the client.
	 */
	protected abstract function GetUpdates($clientVersion);

	/**
	 * Triggers the generation of the update body for the client.
	 *
	 * @param string $clientVersion The version string of the client.
	 */
	protected abstract function OutputBodyXml($clientVersion);

	/**
	 * Generates an update list for a client with the given version string.
	 *
	 * @param string $clientVersion The version string of the client.
	 */
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
	
	protected function OutputUpdates($clientVersion)
	{
		$updates = $this->GetUpdates($clientVersion);

		$lastItemType = null;
		foreach ($updates as $update)
		{
			//Output the closing type tag and open a new type tag for the new update type.
			if ($update->Type != $lastItemType)
			{
				if ($lastItemType !== null)
					printf('	</%s>' . "\n", $lastItemType);
				printf('	<%s>' . "\n", $update->Type);
				$lastItemType = $update->Type;
			}

			//Print the download entry.
			printf('		<item name="%s" version="%s" publisher="%s" architecture="%s" filesize="%d">%s</item>
', htmlentities($update->Name), $update->Version, htmlentities($update->Publisher->Name), $update->Architecture,
				$update->Filesize, htmlentities($update->GetDisplayedLink()));
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
	
	protected function GetUpdates($clientVersion)
	{
		//Prepare the list of updates
		$pdo = new Database();

		/* This following function uses the following function for version comparisons:

		CREATE FUNCTION GetVerMajorMinor(s VARCHAR(255))
			RETURNS DECIMAL(6,3) DETERMINISTIC
		RETURN CAST(
			SUBSTRING_INDEX(s, '.', 2)
			AS DECIMAL(6,3)
		);

		CREATE FUNCTION GetVerReleaseBuild(s VARCHAR(255))
		RETURNS DECIMAL(6,3) DETERMINISTIC
		RETURN CAST(
			CASE
				WHEN LOCATE('.', s) = 0 THEN NULL
				WHEN LOCATE('.', s, LOCATE('.', s)+1) = 0 THEN SUBSTRING_INDEX(s, '.', -1)
				ELSE SUBSTRING_INDEX(s, '.', -2)
			END
			AS DECIMAL(6,3)
		);

		*/

		$statement = $pdo->prepare('SELECT DownloadID
			FROM downloads
			WHERE
				(Superseded = 0) AND
				(`Type` <> \'build\') AND
				(
					(MinVersion IS NULL AND MaxVersion IS NULL) OR
					(MinVersion IS NULL AND																-- Version < MaxVersion
						(
							GetVerMajorMinor(MaxVersion) > :VersionMajorMinor OR						-- a.b > a.b
							(GetVerMajorMinor(MaxVersion) = :VersionMajorMinor AND						-- a.b = a.b; c.d > c.d
								GetVerReleaseBuild(MaxVersion) > :VersionReleaseBuild)
						)
					) OR
					(																					-- Version >= MinVersion
						(
							GetVerMajorMinor(MinVersion) < :VersionMajorMinor OR						-- a.b < a.b
							(GetVerMajorMinor(MinVersion) = :VersionMajorMinor AND						-- a.b = a.b; c.d <= c.d
								GetVerReleaseBuild(MinVersion) <= :VersionReleaseBuild)
						) AND
						MaxVersion IS NULL
					) OR
					(MinVersion <= :Version AND MaxVersion > :Version)
				)
			ORDER BY `Type` ASC');

		$clientComponents = explode('.', $clientVersion);
		$versionMajorMinor = floatval(implode('.', array_splice($clientComponents, 0, 2)));
		$versionReleaseBuild = floatval(implode('.', $clientComponents));
		$statement->bindParam('Version', $clientVersion);
		$statement->bindParam('VersionMajorMinor', $versionMajorMinor);
		$statement->bindParam('VersionReleaseBuild', $versionReleaseBuild);
		$statement->execute();

		$result = array();
		$downloadId = null;
		$statement->bindColumn('DownloadID', $downloadId);
		while ($statement->fetch())
			$result[] = new Download($downloadId);

		return $result;
	}
	
	private function OutputMirrors()
	{
		//Output the list of mirrors
		$pdo = new Database();
		$statement = $pdo->query('SELECT * FROM mirrors ORDER By Continent, Country, City');
		echo '	<mirrors>
		<mirror location="(automatically decide)">http://downloads.sourceforge.net/eraser/</mirror>
';

		$statement->bindColumn('City', $city);
		$statement->bindColumn('Country', $country);
		$statement->bindColumn('URL', $url);
		while ($statement->fetch())
			printf('		<mirror location="%s, %s">%s</mirror>' . "\n", $city, $country, $url);
		echo '	</mirrors>' . "\n";
	}

	protected function OutputBodyXml($clientVersion)
	{
		$this->OutputMirrors();
		$this->OutputUpdates($clientVersion);
	}
}

class UpdateList1_1 extends UpdateListBase
{
	protected function GetVersion()
	{
		return "1.1";
	}
	
	protected function GetUpdates($clientVersion)
	{
		//Prepare the list of updates
		$pdo = new Database();
		$statement = $pdo->prepare('SELECT DownloadID, `Type` as DownloadType
			FROM downloads
			WHERE
				(Superseded = 0) AND
				(
					(															-- Program Updates
						(`Type` <> \'build\') AND
						(MinVersion IS NULL AND MaxVersion IS NULL) OR
						(MinVersion IS NULL AND MaxVersion > :Version) OR
						(MinVersion <= :Version AND MaxVersion IS NULL) OR
						(MinVersion <= :Version AND MaxVersion > :Version)
					) OR
					(															-- Nightly builds greater than our version
						(`Type` = \'build\') AND
						(Version > :Version)
					)
				)
			ORDER BY `Type` ASC');
		$statement->bindParam('Version', $clientVersion);
		$statement->execute();

		$result = array();
		$downloadId = null;
		$downloadType = null;
		$statement->bindColumn('DownloadID', $downloadId);
		$statement->bindColumn('DownloadType', $downloadType);
		while ($statement->fetch())
			$result[] = $downloadType == 'build' ?
				new Build($downloadId) : new Download($downloadId);

		return $result;
	}
	
	protected function OutputBodyXml($clientVersion)
	{
		$this->OutputUpdates($clientVersion);
	}
}
?>
