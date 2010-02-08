<?php
require('../database.php');
require('../updates.php');

$action = $_GET['action'];
$version = $_GET['version'];
$versionMatch = array();
if (empty($action) || empty($version) || !preg_match('/([0-9]+).([0-9]+).([0-9]+).([0-9]+)/', $version, $versionMatch))
	exit;

header('content-type: application/xml');
$downloadsList = intval($versionMatch[2]) == 0 ? new UpdateList1() : new UpdateList1_1();
$downloadsList->Output($version);
?>
