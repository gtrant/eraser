<?php
/**
 * Publishes a Bitten build to the public build server.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

if (count($argv) < 4)
	die('There are insufficient arguments for BuildPublish.php.' . "\n\n" . 'Usage: BuildPublish.php <branch name> <revision> <installer>');

require_once('Credentials.php');
require_once('Build.php');

/**
 * Uploads the contents of a stream to a particular URL.
 *
 * @param string $url      The URL of the destination of the upload.
 * @param string $stream   The source stream for upload.
 * @param string $username The username of the user, if required.
 * @param string $password The password of the user, if required.
 */
function Upload($url, $stream, $username = '', $password = '')
{
	printf('Uploading file to %s... ', $url);

	$curl = curl_init($url);
	curl_setopt($curl, CURLOPT_USERPWD, sprintf('%s:%s', $username, $password));
	curl_setopt($curl, CURLOPT_UPLOAD, true);
	curl_setopt($curl, CURLOPT_INFILE, $stream);

	if (curl_exec($curl) === false)
		throw new Exception('cURL Error: ' . curl_error($curl));
	curl_close($curl);

	printf("File uploaded.\n");
}

function Delete($url, $username = '', $password = '')
{
	printf('Deleting %s... ', $url);

	$curl = curl_init($url);
	curl_setopt($curl, CURLOPT_USERPWD, sprintf('%s:%s', $username, $password));

	//Parse the URL to get the path to delete
    $path = parse_url($url, PHP_URL_PATH);
    curl_setopt($curl, CURLOPT_POSTQUOTE, array(sprintf('rm "%s"', $path)));

	if (curl_exec($curl) === false)
		throw new Exception('cURL Error: ' . curl_error($curl));
	curl_close($curl);

	printf("File deleted.\n");
}

$file = fopen($argv[3], 'rb');
if (!$file)
	die('The file ' . $argv[3] . ' could not be opened for reading.');

try
{
	//Generate a filename for the installer.
	$branches = BuildBranch::Get();
	if (!array_key_exists($argv[1], $branches))
		die('The branch ' . $argv[1] . ' does not exist.');

	$branch = $branches[$argv[1]];
	$pathInfo = pathinfo($argv[3]);
	$fileName = sprintf('Eraser %s.%d.%s', $branch->Version, $argv[2], $pathInfo['extension']);
	$uploadURL = sprintf('sftp://web.sourceforge.net/home/groups/e/er/eraser/htdocs/builds/%s/%s',
		$branch->ID, $fileName);
	Upload($uploadURL, $file, $sftp_username, $sftp_password);
    Delete($uploadURL, $sftp_username, $sftp_password);
}
catch (Exception $e)
{
	die($e->getMessage());
}

fclose($file);
?>
