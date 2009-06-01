<?php
require('../scripts/database.php');

$action = $_GET['action'];
$version = $_GET['version'];
if (empty($action) || empty($version) || !eregi('([0-9]+).([0-9]+).([0-9]+).([0-9]+)', $version))
	exit;

header('content-type: application/xml');
echo '<?xml version="1.0"?>
<updateList version="1.0">' . "\n";

//Output the list of mirrors
$query = mysql_query('SELECT * FROM mirrors ORDER By Continent, Country, City');
echo '	<mirrors>
		<mirror location="(automatically decide)">http://downloads.sourceforge.net/eraser/</mirror>' . "\n";
while ($row = mysql_fetch_array($query))
{
	printf('		<mirror location="%s, %s">%s</mirror>' . "\n", $row['City'], $row['Country'],
		$row['URL']);
}
echo '	</mirrors>';

//Prepare the list of updates
$query = mysql_query(sprintf('SELECT downloads.*, publishers.Name as PublisherName
	FROM downloads
	INNER JOIN publishers ON
		downloads.PublisherID=publishers.PublisherID
	WHERE
		(Superseded = 0) AND
		(
			(MinVersion IS NULL AND MaxVersion IS NULL) OR
			(MinVersion IS NULL AND MaxVersion > \'%1$s\') OR
			(MinVersion <= \'%1$s\' AND MaxVersion IS NULL) OR
			(MinVersion <= \'%1$s\' AND MaxVersion > \'%1$s\')
		)
	ORDER BY `Type` ASC', $version));

$lastItemType = null;
while ($row = mysql_fetch_array($query))
{
	if ($row['Type'] != $lastItemType)
	{
		if ($lastItemType !== null)
			printf('	</%s>' . "\n", $lastItemType);
		printf('	<%s>' . "\n", $row['Type']);
		$lastItemType = $row['Type'];
	}

	//Get the link to the download. We got three forms, relative, absolute and query.
	//Relative links are mirrored by SF.
	//Absolute links... are absolute links.
	//Query links are prefixed with ?, they are handled by download.php on the Eraser website.
	if (substr($row['Link'], 0, 1) == '?')
		$link = 'http://' . $_SERVER['SERVER_NAME'] . '/download.php?id=' . $row['DownloadID'];
	else
		$link = $row['Link'];
	printf('		<item name="%s" version="%s" publisher="%s" architecture="%s" filesize="%d">%s</item>
', htmlentities($row['Name']), $row['Version'], htmlentities($row['PublisherName']), $row['Architecture'],
			$row['Filesize'], htmlentities($link));
}

if (!empty($lastItemType))
	printf('	</%s>' . "\n", $lastItemType);
echo '</updateList>
';
?>
