<?php
/**
 * Defines the Publisher class.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

require_once('Database.php');

/**
 * Represents a Publisher of a download.
 */
class Publisher
{
	private $ID;
	private $Name;
	private $Contact;

	/**
	 * Constructor.
	 *
	 * @param int $publisherId The ID of the Publisher.
	 */
	public function __construct($publisherId)
	{
		$pdo = new Database();
		$statement = $pdo->prepare('SELECT Name, Contact FROM publishers WHERE PublisherID=?');
		$statement->bindParam(1, $publisherId);
		$statement->execute();

		$statement->bindColumn('Name', $this->Name);
		$statement->bindColumn('Contact', $this->Contact);
		if (!$statement->fetch())
			throw new Exception('The given Publisher could not be found.');

		$this->ID = intval($publisherId);
    }

	public function __get($name)
	{
		return $this->$name;
	}
}

?>