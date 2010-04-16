<?php
/**
 * Defines the necessary database connections.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

require_once('Credentials.php');

mysql_connect('localhost', $GLOBALS['database_username'], $GLOBALS['database_password']) or die(mysql_error());
mysql_select_db('eraser') or die(mysql_error());

function PhpToMySqlTimestamp($timestamp)
{
	return date('Y-m-d H:i:s', $timestamp);
}

function MySqlToPhpTimestamp($timestamp)
{
	return strtotime($timestamp);
}

/**
 * PDO-derived database class which handles database connections.
 */
class Database extends PDO
{
	public function __construct()
	{
		parent::__construct('mysql:host=localhost;dbname=eraser', $GLOBALS['database_username'], $GLOBALS['database_password']);
		$this->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
	}
}
?>
