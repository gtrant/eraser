<?php
/**
 * Publishes a Bitten build to the public build server.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

if (count($argv) < 4)
{
	echo 'There are insufficient arguments for BuildPublish.php.' . "\n\n" .
		'Usage: BuildPublish.php <branch name> <revision> <installer>';
	exit(1);
}

require_once('Credentials.php');
require_once('Database.php');
require_once('Build.php');

$file = fopen($argv[3], 'rb');
if (!$file)
{
	echo 'The file ' . $argv[3] . ' could not be opened for reading.';
	exit(1);
}

try
{
	//Generate a filename for the installer.
	$branches = BuildBranch::Get();
	if (!array_key_exists($argv[1], $branches))
		throw new Exception('The branch ' . $argv[1] . ' does not exist.');

	define('SHELL_WEB_ROOT', 'sftp://web.sourceforge.net/home/groups/e/er/eraser/htdocs');
	define('HTTP_WEB_ROOT', 'http://eraser.sourceforge.net');

	$branch = $branches[$argv[1]];
	$pathInfo = pathinfo($argv[3]);
	$fileName = sprintf('Eraser %s.%d.%s', $branch->Version, $argv[2], $pathInfo['extension']);
	$installerPath = sprintf('/builds/%s/%s', $branch->ID, $fileName);

	//Then upload the installer to the URL.
	Upload(SHELL_WEB_ROOT . $installerPath, $file, $sftp_username, $sftp_password);

	//Insert the build to the database.
	printf('Inserting build into database... ');
	Build::CreateBuild($branch->ID, intval($argv[2]), filesize($argv[3]), HTTP_WEB_ROOT . $installerPath);
	printf("Inserted.\n");

	//Remove old builds
	printf('Removing old builds from database...' . "\n");

	$pdo = new Database();
	$statement = $pdo->prepare('UPDATE downloads SET Superseded=1 WHERE DownloadID=?');

	$builds = Build::GetActive($branch->ID);
	for ($i = 0, $j = count($builds) - 3; $i < $j; ++$i)
	{
		printf("\n\t" . 'Removing build %s' . "\n\t\t", $builds[$i]->Name);

		//Delete the copy on the SourceForge web server.
		Delete(SHELL_WEB_ROOT . parse_url($builds[$i]->Link, PHP_URL_PATH), $sftp_username,
			$sftp_password);

		//Remove from the database
		$statement->execute(array($builds[$i]->ID));
	}
}
catch (Exception $e)
{
	echo $e->getMessage();
	exit(1);
}

fclose($file);
?>
