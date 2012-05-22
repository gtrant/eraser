<?php
/**
 * Defines the SourceForge class.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

/**
 * Provides helper functions to access SourceForge data.
 */
class SourceForge
{
	private static $CacheDir = null;

	public static function Init()
	{
		SourceForge::$CacheDir = dirname(__FILE__) . '/cache/SourceForge';
		if (!file_exists(SourceForge::$CacheDir))
			mkdir(SourceForge::$CacheDir, null, true);
	}

	/**
	 * Gets the number of downloads for the given URL.
	 *
	 * @param string $url The URL of the download. The URL must be decoded already (literal comparison)
	 * @return int        The number of downloads.
	 */
	public static function GetDownloads($url)
	{
		//Parse the URL to get the path to the download.
		$urlInfo = parse_url($url);

		//Check the host of the URL.
		if (!in_array(strtolower($urlInfo['host']), array('sourceforge.net', 'www.sourceforge.net')))
			throw new Exception('The given URL is not a SourceForge URL.');

		//Get the path.
		$pathComponents = explode('/', $urlInfo['path']);

		//Retain all path components except the final /download
		$downloadPage = implode('/', array_merge(array_slice($pathComponents, 0, -1), array('stats', 'json')));
		$downloadPage .= '?start_date=2001-10-04&end_date=' . date('Y-m-d');

		//Get the JSON and parse it
		$document = json_decode(SourceForge::Download('http://sourceforge.net' . $downloadPage, 60 * 60));
		if (!empty($document->total))
		{
			return intval($document->total);
		}

		throw new Exception('Could not find the node containing the number of downloads.');
	}

	/**
	 * Downloads the contents of the given URL, storing it in our cache and returning it to the
	 * caller as the return result. The result returned will be cached if the age of the cache
	 * is less than the $age parameter.
	 *
	 * @param string $url The fully-qualified URL to download.
	 * @param int $age    The age of the cache before the URL is downloaded again.
	 * @return string     The contents of the URL.
	 */
	private static function Download($url, $age)
	{
		//Hash the URL to get the cache file name.
		$hash = sha1($url);
		$cacheFile = SourceForge::$CacheDir . '/' . $hash;

		//Does the cache need refreshing?
		if (!file_exists($cacheFile) || time() - filemtime($cacheFile) > $age)
		{
			//Yes, the cache doesn't exist or the cache is stale.
			copy($url, $cacheFile);
		}

		//Return the cached copy.
		return file_get_contents($cacheFile);
	}
}

SourceForge::Init();
?>
