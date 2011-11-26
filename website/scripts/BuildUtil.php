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
	curl_setopt($curl, CURLOPT_USERPWD, sprintf('%s:%s', $username, $password));
	curl_setopt($curl, CURLOPT_FILE, $stream);

	if (curl_exec($curl) === false)
		throw new Exception('cURL Error: ' . curl_error($curl));
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
		throw new Exception('cURL Error: ' . curl_error($curl));
	fclose($stream);
	curl_close($curl);

	printf("File deleted.\n");
}
?>
