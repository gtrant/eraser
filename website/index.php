<?php
require('./scripts/database.php');
function GetDownloads($downloadID)
{
	$query = mysql_query('SELECT COUNT(DownloadID) FROM download_statistics WHERE DownloadID=' . $downloadID);
	$row = mysql_fetch_row($query);
	echo $row ? $row[0] : 'unknown';
}
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"
	"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en"><!-- InstanceBegin template="/Templates/Eraser.dwt" codeOutsideHTMLIsLocked="false" -->
<head>
<!-- InstanceBeginEditable name="Title" -->
<title>Eraser</title>
<!-- InstanceEndEditable -->
<meta http-equiv="content-type" content="text/html; charset=UTF-8" />
<link href="style.css" rel="stylesheet" type="text/css" />
<script type="text/javascript" src="scripts.js"></script>
<!-- InstanceBeginEditable name="head" -->
<style type="text/css">
.downloads {
	margin-left: 1.0em;
}
</style>
<!-- InstanceEndEditable -->
<!-- InstanceParam name="ArticlePoster" type="boolean" value="false" -->
</head>

<body onload="MM_preloadImages('images/btn_home_hov.gif', 'images/btn_download_hov.gif', 'images/btn_forum_hov.gif', 'images/btn_trac_hov.gif')">
<div id="wrap">
	<div id="banner">
		<a href="index.php"><img src="images/header.jpg" class="banner_img" alt="Eraser v6" /></a>
	</div>
	<div id="content">
		<div class="left">
			<!-- InstanceBeginRepeat name="LeftContent" --><!-- InstanceBeginRepeatEntry -->
			<div class="article">
				<div class="article_head">
					<div class="title">
						<h2><!-- InstanceBeginEditable name="LeftContentEditTitle" -->Welcome to the Eraser Home Page!<!-- InstanceEndEditable --></h2>
						
					</div>
				</div>

				<!-- InstanceBeginEditable name="LeftContentEdit" -->
				<p>Eraser is an advanced security tool for Windows which allows you to completely remove sensitive data from your hard drive by overwriting it several times with carefully selected patterns. Works with Windows  98, ME, NT, 2000, XP, Vista, Windows Server 2003 and Server 2008.</p>
				<p>Eraser is Free software and its source code is released under <a href="http://www.fsf.org/licensing/licenses/gpl.html">GNU General Public License</a>.</p>
				<!-- InstanceEndEditable -->
			</div>
			<!-- InstanceEndRepeatEntry --><!-- InstanceBeginRepeatEntry -->
			<div class="article">
				<div class="article_head">
					<div class="title">
						<h2><!-- InstanceBeginEditable name="LeftContentEditTitle" -->Why Use Eraser?<!-- InstanceEndEditable --></h2>
						
					</div>
				</div>

				<!-- InstanceBeginEditable name="LeftContentEdit" -->
				<p>Most people have some data that they would rather not share with others - passwords, personal information, classified documents from work, financial records, self-written poems, the list can be continued forever.</p>
				<p>Perhaps you have saved some of this information on your computer where it is conveniently at your reach, but when the time comes to remove the data from your hard disk, things get a bit more complicated and maintaining your privacy is not as simple as it may have seemed at first.</p>
				<p><strong>Your first thought may be that when you 'delete' the file, the data is gone. Not quite</strong>, when you delete a file, the operating system does not really remove the file from the disk; it only removes the reference of the file from the file system table. The file remains on the disk until another file is created over it, and even after that, it might be possible to recover data by studying the magnetic fields on the disk platter surface.</p>
				<p>Before the file is overwritten, anyone can easily retrieve it with a disk maintenance or an undelete utility.</p>
				<p>There are several problems in secure file removal, mostly caused by the use of write cache, construction of the hard disk and the use of data encoding. These problems have been taken into consideration when Eraser was designed, and because of this intuitive design and a simple user interface, you can safely and easily erase private data from your hard drive.</p>
				<!-- InstanceEndEditable -->
			</div>
			<!-- InstanceEndRepeatEntry --><!-- InstanceBeginRepeatEntry -->
			<div class="article">
				<div class="article_head">
					<div class="title">
						<h2><!-- InstanceBeginEditable name="LeftContentEditTitle" -->Eraser Features<!-- InstanceEndEditable --></h2>
						
					</div>
				</div>

				<!-- InstanceBeginEditable name="LeftContentEdit" -->
				<ul>
					<li>It works with Windows XP, Windows Vista, Windows Server 2003 and Windows Server 2008.</li>
					<ul>
						<li>Windows 98, ME, NT, 2000 can still be used with version 5!</li>
					</ul>
					<li>It works with any drive that works with Windows</li>
					<li>Secure drive erasure methods are supported out of the box</li>
					<li>Erases files, folders and their previous deleted counterparts</li>
					<li>Works with an extremely customisable Scheduler</li>
				</ul>
				<!-- InstanceEndEditable -->
			</div>
			<!-- InstanceEndRepeatEntry --><!-- InstanceEndRepeat -->
		</div>
		<div class="right">
			<div class="right_nav">
				<div class="right_nav_bg">
					<a href="index.php"><img src="images/btn_home.gif" id="nav1" alt="Home" onmouseover="MM_swapImage('nav1','','images/btn_home_hov.gif',1)" onmouseout="MM_swapImgRestore()" /></a><a href="index.php#download"><img src="images/btn_download.gif" id="nav2" alt="Download" onmouseover="MM_swapImage('nav2', '', 'images/btn_download_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a><a href="http://bbs.heidi.ie/viewforum.php?f=30" target="_blank"><img src="images/btn_forum.gif" id="nav3" alt="Forum" onmouseover="MM_swapImage('nav3', '', 'images/btn_forum_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a><a href="trac/"><img src="images/btn_trac.gif" id="nav4" alt="Trac" onmouseover="MM_swapImage('nav4', '', 'images/btn_trac_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a>
				</div>
			</div>
			
			<div class="right_news">
				<h3>Latest News</h3>
				<div class="right_news_bg">
					<h2>Eraser 5.8.7-beta5 released!</h2>
					<div class="posted">Posted by: Joel, 18 April 2009, 11.36am, +800 GMT</div>
					<p>Hello all! I know I've been keeping silence for quite a while, but I assure you Eraser is not dead. <a href="announcements/20090418.html">See the full announcement.</a></p>
					<h2>Eraser 6-rc4 released!</h2>
					<div class="posted">Posted by: Joel, 8 January 2008, 2.45pm, +800 GMT</div>
					<p>Hello again! I wish everyone's been enjoying the new rc3, but while you're at it, I felt it wasn't good enough. So here's <strong>rc4</strong> with more bugfixes than the previous release and with all the v6 goodness. <a href="announcements/20090108.html">See the full announcement.</a></p>
				</div>
			</div>

			<!-- InstanceBeginRepeat name="RightContent" --><!-- InstanceBeginRepeatEntry -->
			<!-- InstanceBeginEditable name="RightContentEdit" -->
			<div class="right_doyouknow">
				<h3>Did you Know?</h3>
				<div class="right_doyouknow_bg">
					<p>Eraser Version 6 Development started on <a href="http://eraser.heidi.ie/trac/changeset/73" target="_blank">November 1, 2007</a>?</p>
				</div>
			</div>
			<!-- InstanceEndEditable -->
			<!-- InstanceEndRepeatEntry --><!-- InstanceBeginRepeatEntry -->
			<!-- InstanceBeginEditable name="RightContentEdit" -->
			<div class="right_l">
				<h3><a name="download" href="javascript: ;"></a><img src="images/ico_download.gif" alt="" />Download Eraser</h3>
				<h4>Stable versions</h4>
				<ul>
					<li><a href="http://downloads.sourceforge.net/eraser/EraserSetup32.exe">Eraser 5.8.6a</a> (x86)</li>
					<li><a href="http://downloads.sourceforge.net/eraser/EraserSetup64.exe">Eraser 5.8.6a</a> (x64)</li>
				</ul>
				<h4>Beta versions</h4>
				<ul>
					<li><a href="announcements/20090108.html">Eraser 6.0.4</a> (rc-4, build 875)<br />
						<span class="downloads">&raquo; downloaded <?php GetDownloads(9); ?> times</span></li>
					<li><a href="announcements/20090418.html">Eraser 5.8.7-beta5</a><br />
						<span class="downloads">&raquo; downloaded <?php GetDownloads(10); ?> times</span></li>
					<li><a href="announcements/20090418.html">Eraser 5.8.7-beta5</a> (portable)
						<span class="downloads">&raquo; downloaded <?php GetDownloads(11); ?> times</span></li>
				</ul>
				<h3>Reviews &amp; Testimonials</h3>
				<ul>
					<li><a href="http://www.chip.de/downloads/c1_downloads_12994923.html" target="_blank">Chip.de</a></li>
					<li><a href="http://www.download.com/Eraser/3000-2092_4-10231814.html" target="_blank">Download.com</a></li>
					<li><a href="reviews.html">See the full list</a></li>
				</ul>
				<h3>Donate</h3>
				<ul>
					<li>Please help out the Eraser team by donating some coffee!</li>
					<!--li><a href="http://www.paypal.com"><img src="images/paypal.jpg" alt="paypall" /></a></li-->
				</ul>
			</div>
			<div class="right_r">
				<h3>The Eraser Team</h3>
				<ul>
					<li>
						<img src="images/usr_admin.gif" alt="" width="17" height="17" />Project Admins:
						<ul>
							<li><a href="http://www.heidi.ie/" target="_blank">Garrett Trant</a> (Admin, Researcher)</li>
							<li><a href="http://www.tolvanen.com/" target="_blank">Sami Tolvanen</a> (Founder)</li>
						</ul>
					</li>
					<li>
						<img src="images/usr_developer.gif" alt="" width="17" height="17" />Developers:
						<ul>
							<li><a href="http://joelsplace.sg/" target="_blank">Joel Low</a> (Lead)</li>
							<li>Kasra Nassiri</li>
							<li><a href="http://lemarquis.deviantart.com/" target="_blank">Dennis van Lith</a> (UX)</li>
						</ul>
					</li>
					<li>
						<img src="images/usr_designer.gif" alt="" width="17" height="17" />Support/Marketing:
						<ul>
							<li>Overwriter (Support)</li>
						</ul>
					</li>
					<li><a href="contributing.html">Interested in helping Eraser?</a></li>
				</ul>
			</div>
			<!-- InstanceEndEditable -->
			<!-- InstanceEndRepeatEntry --><!-- InstanceBeginRepeatEntry -->
			<!-- InstanceBeginEditable name="RightContentEdit" -->
			<div class="right_readings">
				<h3>Related Articles</h3>
				<div class="right_readings_bg">
					<ul>
						<li><a href="http://www.cs.auckland.ac.nz/~pgut001/pubs/secure_del.html" target="_blank">P. Gutmann &mdash; Secure Deletion of Data from Magnetic and Solid-State Memory</a></li>
						<li><a href="http://en.wikipedia.org/wiki/Data_remanence" target="_blank">Wikipedia &mdash; Data Remanence</a></li>
					</ul>
				</div>
			</div>
			<!-- InstanceEndEditable -->
			<!-- InstanceEndRepeatEntry --><!-- InstanceEndRepeat -->
		</div>
	</div>
	<div id="footer">
		<div class="footer">
			<div class="footer_l">
				<p>2008 &copy; Eraser</p>
			</div>
			<div class="footer_r">
				<p>
					Original Design by <a href="http://eatlon.com" target="_blank">Olle Axelsson</a><br />
					Modified for Eraser by <a href="http://lemarquis.deviantart.com" target="_blank">Dennis van Lith</a><br />
					HTML edits by <a href="http://joelsplace.sg" target="_blank">Joel Low</a>
				</p>
			</div>
		</div>
	</div>
</div>
<div id="ffscrollbarfix"></div>
</body>
<!-- InstanceEnd --></html>
