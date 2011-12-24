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
require_once('BuildBranch.php');
require_once('BuildUtil.php');

/**
 * Reads a HTTP response stream and strips it of HTML, returning as a buffer.
 * This is similar to file_get_contents
 * 
 * @param resource $file The file stream to read.
 * @return string The contents of the response, without HTML.
 * @throws Exception An I/O error occurred.
 */
function http_get_contents($file)
{
	$result = '';
	while (($line = fgetss($file, 4096)) !== false)
		$result .= $line . "\n";
	if (!feof($file))
		throw new Exception('Unexpected fgets() failure');

	return $result;
}

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

	//Upload the installer to the URL.
	Upload(SHELL_WEB_ROOT . $installerPath, $file, $sftp_username, $sftp_password);
	
	//Then update our website builds information
	$serverResponse = fopen('php://temp', 'rw');
	try
	{
		Download(sprintf('http://eraser.heidi.ie/scripts/BuildServer.php?branch=%s&revision=%d&filesize=%d&url=%s',
			$argv[1], $argv[2], filesize($argv[3]), urlencode(HTTP_WEB_ROOT . $installerPath)),
			$serverResponse, $build_username, $build_password);
		fseek($serverResponse, 0);
		echo http_get_contents($serverResponse);
		fclose($serverResponse);
	}
	catch (Exception $e)
	{
		fseek($serverResponse, 0);
		$serverResponseText = http_get_contents($serverResponse);
		fclose($serverResponse);
		
		echo 'Error: ' . $e->getMessage();
		if (!empty($serverResponseText))
			echo " with error message:\n" . $serverResponseText . "\n";
		
		Delete(SHELL_WEB_ROOT . $installerPath, $sftp_username, $sftp_password);
		exit(1);
	}
}
catch (Exception $e)
{
	echo $e->getMessage();
	exit(1);
}

fclose($file);
?>
