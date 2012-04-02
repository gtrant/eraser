<?php
/**
 * Defines the Build class.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

require_once('Download.php');

/**
 * Represents a nightly build generated from Trac Slaves.
 */
class Build extends Download
{
	private $Branch;
	private $Revision;
	
	/**
	 * Constructor.
	 *
	 * @param int $buildId The ID of the Build.
	 */
	public function __construct($buildId)
	{
		parent::__construct($buildId);

		$pdo = new Database();
		$statement = $pdo->prepare('SELECT Branch, Revision FROM builds WHERE DownloadID=?');
		$statement->bindParam(1, $this->ID);
		$statement->bindColumn('Branch', $this->Branch);
		$statement->bindColumn('Revision', $this->Revision);
		$statement->execute();
		if ($statement->fetch() === false)
			throw new Exception('The download ' . $buildId . ' is not a build.');
    }

	/**
	 * Gets a Build from the given Branch and Revision.
	 *
	 * @param string $branch The name of the Build branch.
	 * @param int $revision  The revision of the Build.
	 */
	public static function FromBranchAndRevision($branch, $revision)
	{
		$pdo = new Database();
		$statement = $pdo->prepare('SELECT DownloadID FROM builds WHERE Branch=? AND Revision=?');
		$statement->bindParam(1, $branch);
		$statement->bindParam(2, $revision);
		$statement->execute();

		$downloadId = null;
		$statement->bindColumn('DownloadID', $downloadId);
        if ($statement->fetch() === false)
			return null;

		return new Build($downloadId);
	}

	/**
	 * Gets the Builds which are still active for the given branch. The result is ordered by
	 * decreasing age.
	 *
	 * @param string $branch The name of the Build branch.
	 */
	public static function GetActive($branch)
	{
		$pdo = new Database();
		$statement = $pdo->prepare('SELECT builds.DownloadID FROM builds
			INNER JOIN downloads ON builds.DownloadID=downloads.DownloadID
			WHERE downloads.Superseded=0 AND builds.Branch=?
			ORDER BY builds.DownloadID');
		$statement->bindParam(1, $branch);
		$statement->execute();

		$downloadId = null;
		$result = array();
		$statement->bindColumn('DownloadID', $downloadId);
		while ($statement->fetch())
			$result[] = new Build($downloadId);

		return $result;
	}

	public function __get($name)
	{
		return parent::__get($name);
	}
	
	/**
	 * Gets the link to the download that can be referenced publicly.
	 *
	 * @return string The URL to this download.
	 */
	public function GetDisplayedLink()
	{
		return sprintf('http://%s/download.php?id=%d', $_SERVER['SERVER_NAME'], $this->ID);
	}
	
	/**
	 * Creates a new build to be published to the website. This function will first upload
	 * to the public web server before allowing the build to be downloaded.
	 *
	 * @param mixed $branch    The name of the Build branch or a BuildBranch object containing the
	 *                         branch.
	 * @param int $revision    The revision of the Build.
	 * @param int $filesize    The size of the file, in bytes.
	 * @param string $link     The link to the download.
	 */
	public static function CreateBuild($branch, $revision, $filesize, $link)
	{
		$pdo = new Database();
		$pdo->beginTransaction();

		//Define the download properties.
		$statement = $pdo->prepare('INSERT INTO downloads (
				Name, Released, `Type`, Version, PublisherID, Architecture, Filesize, Link
			)
			VALUES
			(
				CONCAT(\'Eraser \', :Version), NOW(), \'build\', :Version, 1,
				\'any\', :Filesize, :Link
			)');

		if (is_string($branch))
		{
			$branches = BuildBranch::Get();
			$branchName = $branch;
			$branch = $branches[$branchName];
			if (!$branch)
				throw new Exception('The provided branch ' . $branchName . ' does not exist.');
		}
		else if (!is_a($branch, 'BuildBranch'))
			throw new Exception('The provided branch is invalid.');

		$statement->bindParam('Version', sprintf('%s.%s', $branch->Version, $revision));
		$statement->bindParam('Filesize', $filesize);
		$statement->bindParam('Link', $link);
		$statement->execute();

		//Define the build properties.
		$statement = $pdo->prepare('INSERT INTO builds (
				DownloadID, Branch, Revision
			) VALUES (
				LAST_INSERT_ID(), ?, ?
			)');
		$branchId = $branch->ID;
		$statement->bindParam(1, $branchId);
		$statement->bindParam(2, $revision);
		$statement->execute();

		//Commit to the database.
		$pdo->commit();
	}
}
?>
