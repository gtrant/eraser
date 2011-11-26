<?php
//HTTP Digest authentication code, modified from http://php.net/manual/en/features.http-auth.php
define('HTTP_DIGEST_REALM', 'Build Server');

//Function to challenge the user
function http_digest_challenge() {
	header('HTTP/1.1 401 Unauthorized');
	header(sprintf('WWW-Authenticate: Digest realm="%s",qop="auth",nonce="%s",opaque="%s"',
		HTTP_DIGEST_REALM, uniqid(), md5(HTTP_DIGEST_REALM)));
	die('Authorisation required.');
}

//Function to parse the HTTP auth header
function http_digest_parse($txt) {
	// protect against missing data
	$needed_parts = array('nonce' => 1,
		'nc' => 1,
		'cnonce' => 1,
		'qop' => 1,
		'username' => 1,
		'uri' => 1,
		'response' => 1
	);
	$data = array();

	preg_match_all('@(\w+)=(?:(?:\'([^\']+)\'|"([^"]+)")|([^\s,]+))@', $txt, $matches, PREG_SET_ORDER);

	foreach ($matches as $m)
	{
		$data[$m[1]] = $m[2] ? $m[2] : ($m[3] ? $m[3] : $m[4]);
		unset($needed_parts[$m[1]]);
	}

	return $needed_parts ? false : $data;
}

//Challenge the client if we did not receive the digest
if (empty($_SERVER['PHP_AUTH_DIGEST']))
	http_digest_challenge();

//Analyze the PHP_AUTH_DIGEST variable
$credentials = http_digest_parse($_SERVER['PHP_AUTH_DIGEST']);
if (!$credentials)
	http_digest_challenge();

//Does the user exist?
require_once('Credentials.php');
require_once('Database.php');
$database = new Database();
$count = $database->query(sprintf('SELECT COUNT(*) FROM build_slaves WHERE Username=%s',
	$database->quote($credentials['username'])))->fetch();
$count = $count[0];
if (!$count)
	http_digest_challenge();

//Check the response for the password.
$password = $database->query(sprintf('SELECT Password FROM build_slaves WHERE Username=%s',
	$database->quote($credentials['username'])))->fetch();
$password = $password['Password'];
$A1 = md5($credentials['username'] . ':' . HTTP_DIGEST_REALM . ':' . $password);
$A2 = md5($_SERVER['REQUEST_METHOD'] . ':' . $credentials['uri']);
$valid_response = md5($A1 . ':' . $credentials['nonce'] . ':' . $credentials['nc'] . ':' .
		$credentials['cnonce'] . ':' . $credentials['qop'] . ':' . $A2);
if ($credentials['response'] != $valid_response)
	http_digest_challenge();

require_once('Build.php');
require_once('BuildUtil.php');
require_once('BuildBranch.php');

try
{
	//Check that we have all the necessary information
	$branches = BuildBranch::Get();
	if (!is_numeric($_GET['revision']) || !is_numeric($_GET['filesize']) || empty($_GET['url']) || empty($_GET['branch']))
		throw new Exception('Invalid build information provided.');
	if (!array_key_exists($_GET['branch'], $branches))
		throw new Exception('The branch ' . $_GET['branch'] . ' does not exist.');

	//Get the branch the notification is for
	$branch = $branches[$_GET['branch']];

	//Insert the build to the database.
	ob_start();
	printf('Inserting build into database... ');
	Build::CreateBuild($branch->ID, intval($_GET['revision']), intval($_GET['filesize']), $_GET['url']);
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
	
	header('content-type: text/plain');
	ob_end_flush();
}
catch (Exception $e)
{
	ob_end_clean();
	header('500 Internal Server Error');
	echo $e->getMessage();
}
?>
