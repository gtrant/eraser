<?php
if (empty($_POST) || empty($_POST['action']))
	exit;

ob_start();
require('../Database.php');

function GetFunctionNameFromStackTrace($line)
{
	$matches = array();
	if (preg_match('/^([^ 	]+) (.*) ([^ 	]+) (.*):([^ 	]+) ([0-9]+)/', $line, $matches))
	{
	}
	else if (preg_match('/^([^ 	]+) (.*)/', $line, $matches))
	{
	}
	
	return $matches[2];
}

function QueryStatus($stackTrace)
{
	//Check that a similar stack trace hasn't been uploaded.
	$status = 'exists';
	$pdo = new Database();
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

			$stackFrames .= sprintf('(StackFrameIndex=%d AND Function=%s) OR ',
				$stackIndex, $pdo->quote(GetFunctionNameFromStackTrace($stackFrame)));
		}
		
		if (empty($stackFrames))
			continue;
		
		//Query for the list of exceptions containing the given functions
		$statement = $pdo->prepare(sprintf('SELECT DISTINCT(BlackBox_Exceptions.ID) FROM BlackBox_StackFrames
			INNER JOIN BlackBox_Exceptions ON BlackBox_StackFrames.ExceptionID=BlackBox_Exceptions.ID
			WHERE (%s) AND ExceptionDepth=? AND ExceptionType=?',
			substr($stackFrames, 0, strlen($stackFrames) - 4)));
		$statement->bindParam(1, $exceptionDepth);
		$statement->bindParam(2, $exception['exception']);
		$statement->execute();

		if ($statement->rowCount() == 0)
			$status = 'new';
	}
	
	header('Content-Type: application/xml');
	printf('<?xml version="1.0"?>
<crashReport status="%s" />', htmlspecialchars($status));
}

function Upload($stackTrace, $crashReport)
{
	$pdo = new Database();
	$pdo->beginTransaction();

	$statement = $pdo->prepare('INSERT INTO BlackBox_Reports SET IPAddress=?');
	$statement->bindParam(1, sprintf('%u', ip2long($_SERVER['REMOTE_ADDR'])));
	try
	{
		$statement->execute();
	}
	catch (PDOException $e)
	{
		throw new Exception('Could not insert crash report into Reports table', null, $e);
	}

	$reportId = $pdo->lastInsertId();
	$exceptionInsert = $pdo->prepare('INSERT INTO BlackBox_Exceptions
		SET ReportID=?, ExceptionType=?, ExceptionDepth=?');
	$exceptionInsert->bindParam(1, $reportId);
	$stackFrameInsert = $pdo->prepare('INSERT INTO BlackBox_StackFrames SET
		ExceptionID=?, StackFrameIndex=?, Function=?, File=?, Line=?');
	foreach ($stackTrace as $exceptionDepth => $exception)
	{
		$exceptionInsert->bindParam(2, $exception['exception']);
		$exceptionInsert->bindParam(3, $exceptionDepth);
		try
		{
			$exceptionInsert->execute();
		}
		catch (PDOException $e)
		{
			throw new Exception('Could not insert exception into Exceptions table', null, $e);
		}
		
		$exceptionId = $pdo->lastInsertId();
		foreach ($exception as $stackIndex => $stackFrame)
		{
			//Ignore the exception key; that stores the exception type.
			if ((string)$stackIndex == 'exception')
				continue;
			
			//at Eraser.Program.OnGUIInitInstance(Object sender) in D:\Development\Projects\Eraser 6.2\Eraser\Program.cs:line 191
			$matches = array();
			$function = $file = $line = null;
			if (preg_match('/^([^ 	]+) (.*) ([^ 	]+) (.*):([^ 	]+) ([0-9]+)/', $stackFrame, $matches))
			{
				$function = $matches[2];
				$file = $matches[4];
				$line = intval($matches[6]);
			}
			else if (preg_match('/^([^ 	]+) (.*)/', $stackFrame, $matches))
			{
				$function = $matches[2];
			}

			$stackFrameInsert->bindParam(1, $exceptionId);
			$stackFrameInsert->bindParam(2, $stackIndex);
			$stackFrameInsert->bindParam(3, $function);
			$stackFrameInsert->bindParam(4, $file);
			$stackFrameInsert->bindParam(5, $line);
			try
			{
				$stackFrameInsert->execute();
			}
			catch (PDOException $e)
			{
				throw new Exception('Could not insert stack frame into Stack Frames table', null, $e);
			}
		}
	}
	
	$pdo->commit();

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