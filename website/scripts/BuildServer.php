<?php

//HTTP Digest authentication code, modified from http://php.net/manual/en/features.http-auth.php
define('HTTP_DIGEST_REALM', 'Build Server');

//Function to challenge the user
function http_digest_challenge() {
	header('HTTP/1.1 401 Unauthorized');
	header(sprintf('WWW-Authenticate: Digest realm="%s",qop="auth",nonce="%s",opaque="%s"',
		HTTP_DIGEST_REALM, uniqid(), md5(HTTP_DIGEST_REALM)));
	exit;
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

//user => password
$users = array('admin' => 'mypass', 'guest' => 'guest');

//Challenge the client if we did not receive the digest
if (empty($_SERVER['PHP_AUTH_DIGEST']))
	http_digest_challenge();

//Analyze the PHP_AUTH_DIGEST variable
$credentials = http_digest_parse($_SERVER['PHP_AUTH_DIGEST']);
if (!$credentials || !isset($users[$credentials['username']]))
	http_digest_challenge();

//Check the response
$A1 = md5($credentials['username'] . ':' . HTTP_DIGEST_REALM . ':' . $users[$credentials['username']]);
$A2 = md5($_SERVER['REQUEST_METHOD'] . ':' . $credentials['uri']);
$valid_response = md5($A1 . ':' . $credentials['nonce'] . ':' . $credentials['nc'] . ':' .
		$credentials['cnonce'] . ':' . $credentials['qop'] . ':' . $A2);
if ($credentials['response'] != $valid_response)
	http_digest_challenge();


?>
