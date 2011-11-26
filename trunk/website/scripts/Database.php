<?php
/**
 * Defines the necessary database connections.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

require_once('Credentials.php');

/**
 * PDO-derived database class which handles database connections.
 */
class Database extends PDO
{
	public function __construct()
	{
		parent::__construct('mysql:host=localhost;dbname=' . $GLOBALS['database_name'], $GLOBALS['database_username'], $GLOBALS['database_password']);
		$this->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
	}
}
?>
