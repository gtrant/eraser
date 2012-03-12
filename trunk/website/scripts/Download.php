<?php
/**
 * Defines the Download class.
 *
 * @author Joel Low <lowjoel@users.sourceforge.net>
 * @version $Id$
 */

require_once('Database.php');
require_once('Publisher.php');

/**
 * Represents a download for Eraser.
 */
class Download
{
	protected $ID;
	protected $Downloads;
	protected $Superseded;

	protected $Name;
	protected $Released;
	protected $Type;
	protected $Version;
	protected $Publisher;
	protected $Architecture;
	protected $Filesize;
	protected $Link;

	/**
	 * Constructor.
	 *
	 * @param int $downloadId The ID of the Download.
	 */
	public function __construct($downloadId)
	{
		$pdo = new Database();
		$statement = $pdo->prepare('SELECT Name, Superseded, Released, Type, Version, PublisherID,
			Architecture, Filesize, Link FROM downloads WHERE DownloadID=?');
		$statement->bindParam(1, $downloadId);
		$statement->execute();

		//Set the name, release date and supersedence as well as the link.
		$statement->bindColumn('Name', $this->Name);
		$statement->bindColumn('Superseded', $this->Superseded);
		$statement->bindColumn('Released', $this->Released);
		$statement->bindColumn('Type', $this->Type);
		$statement->bindColumn('Version', $this->Version);
		$statement->bindColumn('PublisherID', $this->Publisher);
		$statement->bindColumn('Architecture', $this->Architecture);
		$statement->bindColumn('Filesize', $this->Filesize);
		$statement->bindColumn('Link', $this->Link);
		if ($statement->fetch() === false)
			throw new Exception('The given Download could not be found.');

		//Convert the release date to a DateTime object
		$this->Released = new DateTime($this->Released);

		//Get the number of downloads
		$statement = $pdo->prepare('SELECT COUNT(*) FROM download_log WHERE DownloadID=?');
		$statement->bindParam(1, $downloadId);
		$statement->execute();
		$row = $statement->fetch();
		$this->Downloads = $row ? $row[0] : 0;

		$this->ID = intval($downloadId);
	}

	public function __get($name)
	{
		switch ($name)
		{
			case 'Publisher':
				return new Publisher($this->Publisher);
			default:
				return $this->$name;
		}
	}

	public function __set($name, $value)
	{
		switch ($name)
		{
			case 'Superseded':
				if (!is_bool($value))
					throw new Exception('Download::Superseded expects bool; but $value is not boolean.');

				$pdo = new Database();
				$statement = $pdo->prepare('UPDATE downloads SET Superseded=? WHERE DownloadID=?');
				$statement->bindParam(1, $this->Superseded);
				$statement->bindParam(2, $this->ID);
				$statement->execute();
				break;

			default:
				throw new ErrorException(sprintf('The property %s does not exist or cannot be writte to.', $name));
		}
	}

	/**
	 * Gets the link to the download that can be referenced publicly.
	 *
	 * @return string The URL to this download.
	 */
	public function GetDisplayedLink()
	{
		return sprintf('http://%s/download.php?id=%d', $_SERVER['SERVER_NAME'], $this->ID);
	}

	/**
	 * Initiates the download of the current Download.
	 */
	public function InitiateDownload()
	{
		//Register the download
		$pdo = new Database();
		$statement = $pdo->prepare('INSERT INTO download_log SET DownloadID=?');
		$statement->bindParam(1, $this->ID);
		$statement->execute();

		if (preg_match('/http(s{0,1}):\/\/(.*)/', $this->Link))
		{
			header('location: ' . $this->Link);
		}
		else if (substr($this->Link, 0, 1) == '?')
		{
			//Get a name to call the file.
			$filePath = dirname(__FILE__) . '/../downloads/' . substr($this->Link, 1);
			$pathInfo = pathinfo($filePath);
			$downloadName = empty($pathInfo['extension']) ?
				$this->Name : sprintf('%s.%s', $this->Name, $pathInfo['extension']);

			//Transfer the file.
			$stream = fopen($filePath, 'rb');
			Download::TransferDownload($stream, $downloadName);
		}
		else
		{
			throw new Exception('Unknown download link');
		}
	}

	private static function TransferDownload($stream, $name, $contentType = 'application/octet-stream')
	{
		//Give the content type header
		if (strpos($_SERVER['HTTP_USER_AGENT'], 'Safari') !== false)
			header('Content-Type: ' . $contentType);
		else
			header('Content-Type: application/octet-stream');

		//Calculate the length of the download
		$currentPos = ftell($stream);
		if ($currentPos !== false)
		{
			if (fseek($stream, 0, SEEK_END) == 0)
			{
				header('Content-Length: ' . ftell($stream));
				fseek($stream, $currentPos, SEEK_SET);
			}
		}

		header(sprintf('Content-Disposition: attachment; filename="%s"', $name));
		if (strpos($_SERVER['HTTP_USER_AGENT'], 'MSIE') !== false || strpos($_SERVER['HTTP_USER_AGENT'], 'Safari') !== false)
		{
			//IE browser
			header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
			header('Pragma: public');
		}
		else
		{
			header('Pragma: no-cache');
		}

		fpassthru($stream);
	}
}
?>
