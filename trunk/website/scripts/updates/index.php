<?php
require_once('../UpdateList.php');

$action = empty($_GET['action']) ? null : $_GET['action'];
$version = empty($_GET['version']) ? null : $_GET['version'];
$versionMatch = array();
if (empty($action) || empty($version) || !preg_match('/([0-9]+).([0-9]+).([0-9]+).([0-9]+)/', $version, $versionMatch))
	exit;

ob_start();
header('content-type: application/xml');
$downloadsList = intval($versionMatch[2]) == 0 ? new UpdateList1() : new UpdateList1_1();
$downloadsList->Output($version);

header('content-length: ' . ob_get_length());
?>
