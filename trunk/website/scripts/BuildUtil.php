<?php
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

/**
 * Downloads the contents of a particular URL to a stream.
 * 
 * @param string $url      The URL to download.
 * @param string $stream   The stream to save to.
 * @param string $username The username of the user, if required.
 * @param string $password The password of the user, if required.
 */
function Download($url, $stream, $username = '', $password = '')
{
	printf('Downloading %s... ', $url);

	$curl = curl_init($url);
	curl_setopt($curl, CURLOPT_HTTPAUTH, CURLAUTH_ANYSAFE);
	curl_setopt($curl, CURLOPT_USERPWD, sprintf('%s:%s', $username, $password));
	curl_setopt($curl, CURLOPT_FILE, $stream);

	if (curl_exec($curl) === false)
		throw new Exception('cURL Error: ' . curl_error($curl));
	
	//Check if we have a HTTP error
	$responseCode = curl_getinfo($curl, CURLINFO_HTTP_CODE);
	if ($responseCode === false || intval($responseCode) >= 400)
		throw new Exception('HTTP Error: Server returned HTTP error code ' .
			$responseCode);
	curl_close($curl);

	printf("File downloaded.\n");
}

function Delete($url, $username = '', $password = '')
{
	printf('Deleting %s... ', $url);

	$curl = curl_init($url);
	curl_setopt($curl, CURLOPT_USERPWD, sprintf('%s:%s', $username, $password));

	//Create a temp stream to upload to the old path (to zero the length)
	$stream = fopen('php://temp', 'r');
	curl_setopt($curl, CURLOPT_UPLOAD, true);
	curl_setopt($curl, CURLOPT_INFILE, $stream);

	//Parse the URL to get the path to delete
	$path = parse_url($url, PHP_URL_PATH);
	curl_setopt($curl, CURLOPT_POSTQUOTE, array(
		sprintf('rm "%s"', $path)
	));

	if (curl_exec($curl) === false)
	{
		$scheme = parse_url($url, PHP_URL_SCHEME);
		if (curl_errno($curl) == CURLE_UNSUPPORTED_PROTOCOL && $scheme == 'sftp')
			return Delete_Sftp($url, $username, $password);
		
		throw new Exception('cURL Error ' . $scheme.curl_errno($curl) . ': '. curl_error($curl));
	}
	fclose($stream);
	curl_close($curl);

	printf("File deleted.\n");
}

function Delete_Sftp($url, $username = '', $password = '')
{
	echo 'Trying alternative method... ';
	$urlInfo = parse_url($url);
	
	//Connect to the server and authenticate the server.
	$ssh2 = ssh2_connect($urlInfo['host'], $urlInfo['port']);
	$knownFingerprints = array(
		'b0a8eb30ce1a0e6a4d7a6b3a0ac62760'
	);
	if (!in_array($knownFingerprints, ssh2_fingerprint($ssh2, SSH2_FINGERPRINT_SHA1 | SSH2_FINGERPRINT_HEX)))
	{
		throw new Exception(sprintf('Authentication Error: The fingerprint provided ' .
			'by %s is not recognised, disconnecting.', $urlInfo['host']));
	}
	
	//Authenticate ourselves
	if (!ssh2_auth_password($ssh2, $username, $password))
	{
		throw new Exception('Authentication Error: The credentials provided are incorrect.');
	}
	
	//Delete the file
	$sftp = ssh2_sftp($ssh2);
	if (!ssh2_sftp_unlink($sftp, $urlInfo['path']))
	{
		throw new Exception(sprintf('SFTP Error: Could not delete file %s.', $urlInfo['path']));
	}
	
	printf("File deleted.\n");
}
?>
