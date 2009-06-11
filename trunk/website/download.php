<?php
require('./scripts/database.php');
if (empty($_GET['id']))
	exit;

//Get the download link associated with the download
$query = mysql_query(sprintf('SELECT Link, Superseded FROM downloads WHERE DownloadID=%d', intval($_GET['id'])));
if (!$query)
	exit;
if (!($row = mysql_fetch_array($query)))
	exit;

//Check for supercedence
if (intval($row['Superseded']))
{
	echo 'The requested download has been superseded with a newer version.';
	exit;
}

//Register the download
mysql_query(sprintf('INSERT INTO download_statistics (DownloadID) VALUES (%d)', intval($_GET['id'])));

if (eregi('http(s{0,1})://)(.*)', $row['Link']))
	header('location: ' . $row['Link']);
else if (substr($row['Link'], 0, 1) == '?')
{
	$fileName = substr($row['Link'], 1);
	header("Content-Type: application/octet-stream");
	header('Content-Length: ' . filesize('./downloads/' . $fileName));
	if (strpos($_SERVER['HTTP_USER_AGENT'], "MSIE") !== false)
	{
		//IE browser
		header('Content-Disposition: inline; filename="' . $fileName . '"');
		header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
		header('Pragma: public');
	}
	else
	{
		header('Content-Disposition: attachment; filename="' . $fileName . '"');
		header('Pragma: no-cache');
	}

	echo file_get_contents('./downloads/' . $fileName);
}
?>
