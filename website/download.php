<?php
require('scripts/downloads.php');

if (!empty($_GET['id']))
{
	$download = Build::GetBuildFromID(intval($_GET['id']));
	if (empty($download))
		$download = new Download(intval($_GET['id']));
	
	//Check for supersedence
	if ($download->Superseded)
	{
		header('location: ' . $_SERVER['PHP_SELF'] . '?error=' . urlencode('The requested download has been superseded with a newer version.'));
		exit;
	}
	
	$download->InitiateDownload();
	exit;
}
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"
	"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en"><!-- InstanceBegin template="/Templates/Eraser.dwt" codeOutsideHTMLIsLocked="false" -->
<head>
<!-- InstanceBeginEditable name="Title" -->
<title>Eraser :: Downloads</title>
<!-- InstanceEndEditable -->
<meta http-equiv="content-type" content="text/html; charset=UTF-8" />
<link href="style.css" rel="stylesheet" type="text/css" />
<script type="text/javascript" src="scripts.js"></script>
<!-- InstanceBeginEditable name="head" --><!-- InstanceEndEditable -->
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
						<h2><!-- InstanceBeginEditable name="LeftContentEditTitle" --><a href="index.php">Eraser</a> Downloads<!-- InstanceEndEditable --></h2>
						
					</div>
				</div>

				<!-- InstanceBeginEditable name="LeftContentEdit" -->
				<?php if (!empty($_GET['error'])) printf('<p class="error">%s</p>', $_GET['error']); ?>
				<p>Thank for you having interest in Eraser. Eraser is available in a few flavours, the stable, the beta as well as the nightly builds.</p>
				<p>Stable builds of Eraser are builds in which few, if any, bugs remain in the code and is suitable for use in all environments. If in doubt, choose the Stable version. The beta and nightly builds cater to a slightly different audience. Beta and nightly builds are built on the previous stable version released, but may contain new features or bug fixes to bugs discovered in the stable builds. Use the Beta and Nightly builds at your own risk<sup><a href="#footnote1" name="footnote1Src">1</a></sup>. </p>
				<p>If you do discover bugs in the nightly builds, report them on <a href="trac/newticket">Trac</a>, citing the build number that you have used (the number after 'r', it can also be found in the About dialog for Eraser 6, it is <em>d</em> value in the version number <em>a.b.c.d</em>).</p>
				<table>
					<tr>
						<td colspan="4"><h3>Stable Builds</h3></td>
					</tr>
					<tr>
						<th>Build Name</th>
						<th>Version</th>
						<th>Release Date</th>
						<th>&nbsp;</th>
					</tr>
					<tr>
						<td><a href="http://sourceforge.net/projects/eraser/files/Eraser%206/Eraser%206.0.6.1376.exe/download">Eraser 6.0.6.1376</a></td>
						<td>6.0.6.1376</td>
						<td>15/12/09 10:15am</td>
						<td>&nbsp;</td>
					</tr>
					<tr>
						<td><a href="http://sourceforge.net/projects/eraser/files/Eraser%205/5.8.8/Eraser%205.8.8.exe/download">Eraser 5.8.8</a></td>
						<td>5.8.8</td>
						<td>16/12/09 2:00pm</td>
						<td>&nbsp;</td>
					</tr>
					<tr>
						<td><a href="http://sourceforge.net/projects/eraser/files/Eraser%205/5.8.8/Eraser%205.8.8%20Portable.zip/download">Eraser 5.8.8</a><br /><em>portable</em></td>
						<td>5.8.8</td>
						<td>17/12/09 5:30pm</td>
						<td>&nbsp;</td>
					</tr>
					<tr>
						<td><a href="http://downloads.sourceforge.net/eraser/Eraser57Setup.zip">Eraser 5.7</a><br /><em>for Windows 9x/Me</em></td>
						<td>5.7</td>
						<td>4/9/03 11:35pm</td>
						<td>&nbsp;</td>
					</tr>
					<tr>
						<td colspan="4"><h3>Beta Builds</h3></td>
					</tr>
					<tr>
						<th>Build Name</th>
						<th>Version</th>
						<th>Release Date</th>
						<th>Downloads</th>
					</tr>
					<tr><?php $download = new Download(13); ?>
						<td><a href="announcements/20090706.html">Eraser 5.8.8</a> (beta1)</td>
						<td>5.8.8-beta1</td>
						<td><?php echo date('j/n/y g:ia', $download->Released); ?></td>
						<td><?php echo $download->Downloads; ?></td>
					</tr>
					<tr><?php $download = new Download(12); ?>
						<td><a href="announcements/20090610.html">Eraser 6.0.5</a> (rc-5, build 1114)</td>
						<td>6.0.5.1114 (rc5)</td>
						<td><?php echo date('j/n/y g:ia', $download->Released); ?></td>
						<td><?php echo $download->Downloads; ?></td>
					</tr>
					<tr>
						<td colspan="4">
							<div style="position: relative; width: 100%">
								<div style="position: absolute; text-align: right; width: 100%; height: 100%; padding: 4px 2px 4px 0">
									<a href="trac/timeline?changeset=on">(view changelog)</a>
								</div>
								<h3>Nightly Builds</h3>
							</div>
						</td>
					</tr>
					<tr>
						<th>Build Name</th>
						<th>Revision</th>
						<th>Build Date</th>
						<th>Downloads</th>
					</tr>
<?php
					$builds = Build::Get();
					foreach ($builds as $buildName => $build)
					{
?>
					<tr>
						<td colspan="4"><h4><?php echo $buildName; ?></h4></td>
					</tr>
<?php
						foreach ($build as $revision)
						{
							if ($revision->Superseded)
								continue;
?>
					<tr>
						<td><a href="<?php echo $_SERVER['PHP_SELF'] . '?id=' . $revision->ID; ?>"><?php echo $revision->Name; ?></a></td>
						<td>r<?php echo $revision->Revision; ?></td>
						<td><?php echo date('j/n/y g:ia', $revision->Released); ?></td>
						<td><?php echo $revision->Downloads; ?></td>
					</tr>
<?php
						}
					}
?>
				</table>
				<div style="height: 150px">&nbsp;</div><hr />
				<p><a href="#footnote1Src" name="footnote1"></a>Disclaimer: The security of the erasures has not been verified by internal or external entities. The code may be still of <em>beta quality</em> and may not remove all traces of files. If you have security concerns, do use the stable versions.</p>
				<!-- InstanceEndEditable -->
			</div>
			<!-- InstanceEndRepeatEntry --><!-- InstanceEndRepeat -->
		</div>
		<div class="right">
			<div class="right_nav">
				<div class="right_nav_bg">
					<a href="index.php"><img src="images/btn_home.gif" id="nav1" alt="Home" onmouseover="MM_swapImage('nav1','','images/btn_home_hov.gif',1)" onmouseout="MM_swapImgRestore()" /></a><a href="download.php"><img src="images/btn_download.gif" id="nav2" alt="Download" onmouseover="MM_swapImage('nav2', '', 'images/btn_download_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a><a href="http://bbs.heidi.ie/viewforum.php?f=30" target="_blank"><img src="images/btn_forum.gif" id="nav3" alt="Forum" onmouseover="MM_swapImage('nav3', '', 'images/btn_forum_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a><a href="trac/"><img src="images/btn_trac.gif" id="nav4" alt="Trac" onmouseover="MM_swapImage('nav4', '', 'images/btn_trac_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a>
				</div>
			</div>
			
			<div class="right_news">
				<h3>Latest News</h3>
				<div class="right_news_bg">
					<h2>Eraser 6 Stable released!</h2>
					<div class="posted">Posted by: Joel, 15<sup>th</sup> December 2009, 10:15 am +800GMT</div>
					<p>Eraser 6 stable has been released after being for  2 years in development. Eraser 6 sports a completely revamped user interface, and Windows XP/Vista/7 support out of the box. <a href="announcements/20091215.html">See the full announcement</a></p>
					<h2>Eraser 5.8.8 released!</h2>
					<div class="posted">Posted by: Joel, 15<sup>th</sup> December 2009, 10:15 am +800GMT</div>
					<p>Eraser 5.8.8 is released. This release is mainly meant to fix the disk corruption which results from running a first/last 2KB erase on a sparse file as well as&nbsp;to fix the context menu causing Explorer to hang when&nbsp;installed with certain software which also inserts a context menu item. <a href="announcements/20091215.html">See the full announcement.</a></p>
					<h2>Eraser 6-rc5 released!</h2>
					<div class="posted">Posted by: Joel, 10 June 2009, 7.00pm, +800 GMT</div>
					<p>Having written almost 25,000 lines of code since the start of our project, v6 was due for a code review. So that's what I did, reviewed the code with the help of a static code analysis tool (FxCop for you developers out there :) ) and fixed all sorts of inconsistencies in the code. This should result in slightly higher performance and better behaviour when Eraser is in use. <a href="announcements/20090610.html">See the full announcement.</a></p>
				</div>
			</div>

			<!-- InstanceBeginRepeat name="RightContent" --><!-- InstanceEndRepeat -->
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
