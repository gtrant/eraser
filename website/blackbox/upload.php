<?php
if (empty($_POST) || empty($_POST['action']))
	exit;

ob_start();
require('../scripts/database.php');

function GetFunctionNameFromStackTrace($line)
{
	$matches = array();
	if (preg_match('/^at (.*) in (.*):line ([0-9]+)/', $line, $matches))
	{
	}
	else if (preg_match('/^at (.*)/', $line, $matches))
	{
	}
	
	return $matches[1];
}

function QueryStatus($stackTrace)
{
	//Check that a similar stack trace hasn't been uploaded.
	$status = 'exists';
	foreach ($stackTrace as $exceptionDepth => $exception)
	{
		if ($status != 'exists')
			break;

		$stackFrames = '';
		foreach ($exception as $stackIndex => $stackFrame)
		{
			//Ignore the exception key; that stores the exception type.
			if ($stackIndex == 'exception')
				continue;

			$stackFrames .= sprintf('(StackFrameIndex=%d AND Function=\'%s\') OR ',
				$stackIndex, mysql_real_escape_string(GetFunctionNameFromStackTrace($stackFrame)));
		}
		
		if (empty($stackFrames))
			continue;
		
		//Query for the list of exceptions containing the given functions
		$query = mysql_query(sprintf('SELECT DISTINCT(BlackBox_Exceptions.ID) FROM BlackBox_StackFrames
			INNER JOIN BlackBox_Exceptions ON BlackBox_StackFrames.ExceptionID=BlackBox_Exceptions.ID
			WHERE (%s) AND ExceptionDepth=%d AND ExceptionType=\'%s\'',
			substr($stackFrames, 0, strlen($stackFrames) - 4), $exceptionDepth,
			mysql_real_escape_string($exception['exception'])));
		
		if (mysql_num_rows($query) == 0)
			$status = 'new';
	}
	
	header('Content-Type: application/xml');
	printf('<?xml version="1.0"?>
<crashReport status="%s" />', htmlspecialchars($status));
}

function Upload($stackTrace, $crashReport)
{
	mysql_query('BEGIN TRANSACTION');
	mysql_query(sprintf('INSERT INTO BlackBox_Reports SET IPAddress=%u', ip2long($_SERVER['REMOTE_ADDR'])));
	if (mysql_affected_rows() != 1)
		throw new Exception('Could not insert crash report into Reports table: ' . mysql_error());

	$reportId = mysql_insert_id();
	foreach ($stackTrace as $exceptionDepth => $exception)
	{
		mysql_query(sprintf('INSERT INTO BlackBox_Exceptions SET ReportID=%d, ExceptionType=\'%s\', ExceptionDepth=%d',
			$reportId, mysql_real_escape_string($exception['exception']), $exceptionDepth));
		if (mysql_affected_rows() != 1)
			throw new Exception('Could not insert exception into Exceptions table: ' . mysql_error());

		$exceptionId = mysql_insert_id();
		foreach ($exception as $stackIndex => $stackFrame)
		{
			//Ignore the exception key; that stores the exception type.
			if ($stackIndex == 'exception')
				continue;
			
			//at Eraser.Program.OnGUIInitInstance(Object sender) in D:\Development\Projects\Eraser 6.2\Eraser\Program.cs:line 191
			$matches = array();
			$function = $file = $line = null;
			if (preg_match('/^at (.*) in (.*):line ([0-9]+)/', $stackFrame, $matches))
			{
				$function = $matches[1];
				$file = $matches[2];
				$line = intval($matches[3]);
			}
			else if (preg_match('/^at (.*)/', $stackFrame, $matches))
			{
				$function = $matches[1];
			}
			
			mysql_query(sprintf('INSERT INTO BlackBox_StackFrames SET
				ExceptionID=%d, StackFrameIndex=%d, Function=\'%s\', File=%s, Line=%s',
				$exceptionId, $stackIndex, mysql_real_escape_string($function),
				empty($file) ? 'null' : sprintf('\'%s\'', mysql_real_escape_string($file)),
				$line == null ? 'null' : intval($line)));
			
			if (mysql_affected_rows() != 1)
				throw new Exception('Could not insert stack frame into Stack Frames table: ' . mysql_error());
		}
	}
	
	mysql_query('COMMIT');
	
	//Move the temporary file to out dumps folder for later inspection
	if (!move_uploaded_file($crashReport['tmp_name'], 'dumps/' . $reportId . '.tbz'))
		throw new Exception('Could not store crash dump onto server.');
}

try
{
	switch ($_POST['action'])
	{
		case 'status':
			if (empty($_POST['stackTrace']))
				exit;
			QueryStatus($_POST['stackTrace']);
			break;
	
		case 'upload':
			if (empty($_FILES) || empty($_POST['stackTrace']))
				exit;
			Upload($_POST['stackTrace'], $_FILES['crashReport']);
			break;
	}
}
catch (Exception $e)
{
	ob_end_clean();
	header('HTTP/1.1 500 Internal Server Error');
	printf('<?xml version="1.0"?>
<error>%s</error>', htmlspecialchars($e->getMessage()));
}
?>
