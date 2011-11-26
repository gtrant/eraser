<?php
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
			'Eraser6' => new BuildBranch('Eraser6', 'Eraser 6.0', '6.0.8'),
			'Eraser6.2' => new BuildBranch('Eraser6.2', 'Eraser 6.2', '6.1.0')
		);
	}

	public function __get($name)
	{
		return $this->$name;
	}
}
?>
