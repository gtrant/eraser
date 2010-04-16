<?php
/**
 * Defines the Build class.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id $
 */

require_once('./Download.php');

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
	 * Gets the Builds which are still active for the given branch.
	 *
	 * @param string $branch The name of the Build branch.
	 */
	public static function GetActive($branch)
	{
		$pdo = new Database();
		$statement = $pdo->prepare('SELECT builds.DownloadID FROM builds
			INNER JOIN downloads ON builds.DownloadID=downloads.DownloadID
			WHERE downloads.Superseded=0 AND builds.Branch=?');
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
		return $this->$name;
	}
	
	/**
	 * Creates a new build to be published to the website. This function will first upload
	 * to the public web server before allowing the build to be downloaded.
	 *
	 * @param string $branch   The name of the Build branch.
	 * @param <type> $revision The revision of the Build.
	 */
	public static function CreateBuild($branch, $revision)
	{
		
	}
}

/**
 * Represents a branch of all builds.
 */
class BuildBranch
{
	private $ID;
	private $Title;
	private $Version;

	private function __construct($branchId, $title, $version)
	{
		$this->ID = $branchId;
		$this->Title = $title;
		$this->Version = $version;
	}

	/**
	 * Gets the branches that are currently maintained.
	 *
	 * @return array The list of branches that are currently maintained.
	 */
	public static function Get()
	{
		return array(
			'Eraser6' => new BuildBranch('Eraser6', 'Eraser 6.0', '6.0.6'),
			'Eraser6.2' => new BuildBranch('Eraser6.2', 'Eraser 6.2', '6.1.0')
		);
	}

	public function __get($name)
	{
		return $this->$name;
	}
}
?>
