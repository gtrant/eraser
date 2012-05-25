<?php
if (empty($_POST) || empty($_POST['action']))
	exit;

ob_start();
require('../Database.php');

function GetFunctionNameFromStackTrace($line)
{
	$result = GetStackFrameInformation($line);
	return $result->function;
}

function GetStackFrameInformation($line)
{
	//at Eraser.Program.OnGUIInitInstance(Object sender) in D:\Development\Projects\Eraser 6.2\Eraser\Program.cs:line 191
	$matches = array();
	$function = $file = $line = null;
	if (preg_match('/^([^ 	]+) (.*) ([^ 	]+) (.*):([^ 	]+) ([0-9]+)/', $line, $matches))
	{
		$function = $matches[2];
		$file = $matches[4];
		$line = intval($matches[6]);
	}
	else if (preg_match('/^([^ 	]+) (.*)/', $line, $matches))
	{
		$function = $matches[2];
	}

	return (object)array('function' => $function, 'file' => $file, 'line' => $line);
}

function GetExceptionIDFromExceptionInfo($exception, $exceptionDepth)
{
	$pdo = new Database();
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
		return null;

	//Query for the list of exceptions containing the given functions
	$statement = $pdo->prepare(sprintf('SELECT DISTINCT(blackbox_exceptions.ID) AS ExceptionID FROM blackbox_stackframes
		INNER JOIN blackbox_exceptions ON blackbox_stackframes.ExceptionID=blackbox_exceptions.ID
		WHERE (%s) AND ExceptionDepth=? AND ExceptionType=?',
		substr($stackFrames, 0, strlen($stackFrames) - 4)));
	$statement->bindParam(1, $exceptionDepth);
	$statement->bindParam(2, $exception['exception']);
	$statement->execute();

	if ($statement->rowCount() == 0)
		return false;
	$row = $statement->fetch();
	return $row['ExceptionID'];
}

function QueryStatus($stackTrace)
{
	$status = 'exists';
	$reportID = false;
	$exceptionIDs = array();
	$pdo = new Database();
	
	foreach ($stackTrace as $exceptionDepth => $exception)
	{
		$exceptionID = GetExceptionIDFromExceptionInfo($exception, $exceptionDepth);
		if ($exceptionID === null)
			continue;
		else if ($exceptionID === false)
		{
			$status = 'new';
			break;
		}
		else
			//Store the current exception ID on the stack
			array_push($exceptionIDs, $exceptionID);
	}

	header('Content-Type: application/xml');
	
	//If this is an existing exception, try to find the most similar report.
	if ($status == 'exists' && count($exceptionIDs) > 0)
	{
		$ids = implode(', ', $exceptionIDs);
		$result = $pdo->query(sprintf('SELECT ReportID, COUNT(ID) as Matches FROM blackbox_exceptions
			WHERE ID IN (%s) GROUP BY ReportID ORDER BY Matches DESC', $ids));
		if ($result->rowCount() > 0)
		{
			$row = $result->fetch();
			$reportID = $row['ReportID'];

			printf('<?xml version="1.0"?>
<crashReport status="exists" id="%s" />', htmlspecialchars($status), $reportID);
			return;
		}
	}

	//Otherwise just return the status of the report.
	printf('<?xml version="1.0"?>
<crashReport status="%s" />', htmlspecialchars($status));
}

function Upload($stackTrace, $crashReport)
{
	$pdo = new Database();
	$pdo->beginTransaction();

	$statement = $pdo->prepare('INSERT INTO blackbox_reports SET IPAddress=?');
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
	$exceptionInsert = $pdo->prepare('INSERT INTO blackbox_exceptions
		SET ReportID=?, ExceptionType=?, ExceptionDepth=?');
	$exceptionInsert->bindParam(1, $reportId);
	$stackFrameInsert = $pdo->prepare('INSERT INTO blackbox_stackframes SET
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
			
			$stackFrameInfo = GetStackFrameInformation($stackFrame);

			$stackFrameInsert->bindParam(1, $exceptionId);
			$stackFrameInsert->bindParam(2, $stackIndex);
			$stackFrameInsert->bindParam(3, $stackFrameInfo->function);
			$stackFrameInsert->bindParam(4, $stackFrameInfo->file);
			$stackFrameInsert->bindParam(5, $stackFrameInfo->line);
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
	$localName = $crashReport['name'];
	$lastDot = strrpos($localName, '.');
	if ($lastDot !== false)
		$localExt = substr($localName, strrpos($localName, '.') + 1);
	else
		$localExt = 'bz2';
	if (!move_uploaded_file($crashReport['tmp_name'], 'dumps/' . $reportId . '.tar.' . $localExt))
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