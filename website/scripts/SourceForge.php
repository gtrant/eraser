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
		$CacheDir = dirname(__FILE__) . '/cache';
	}

	/**
	 * Gets the number of downloads for the given URL.
	 *
	 * @param string $url The URL of the download.
	 * @return int        The number of downloads.
	 */
	public static function GetDownloads($url)
	{//projects/eraser/files/Eraser%206/6.0.7/Eraser%206.0.7.1893.exe/download
		//Parse the URL to get the path to the download.
		$urlInfo = parse_url($url);

		//Check the host of the URL.
		if (!in_array(strtolower($urlInfo['host']), array('sourceforge.net', 'www.sourceforge.net')))
			throw new Exception('The given URL is not a SourceForge URL.');

		//Get the path.
		$pathComponents = explode('/', $urlInfo['path']);

		//The first three components is the page we need to download.
		$downloadPage = implode('/', array_slice($pathComponents, 0, 4));

		//Parse the download page
		$document = new DOMDocument();
		printf('Loading ' .'http://sourceforge.net' . $downloadPage . "\n");
		$document->loadHTMLFile('http://sourceforge.net' . $downloadPage);

		foreach ($document->getElementsByTagName('a') as $element)
		{
			if (!is_a($element, 'DOMElement'))
				continue;

			//$parent is the <td> node.
			$parent = $element->parentNode;
			if (is_a($parent, 'DOMElement') && $parent->tagName == 'td' &&
				$parent->getAttribute('class') == 'tree' &&
				$element->getAttribute('href') == implode('/', $pathComponents))
			{
				//$grandParent is the <tr> node.
				$grandParent = $parent->parentNode;

				//Find the download column (currently the 5th)
				$i = 0;
				foreach ($grandParent->getElementsByTagName('td') as $cell)
				{
					if (++$i == 5)
					{
						//We are at the 5th column, return the contents as an integer.
						$result = str_replace(',', '', $cell->textContent);
						return intval($result);
					}
				}
			}
		}

		throw new Exception('Could not find the node containing the download.');
	}
}

SourceForge::Init();
?>
