<?php
require('scripts/Download.php');
require('scripts/Build.php');
require('scripts/SourceForge.php');

if (!empty($_GET['id']))
{
	try
	{
		$download = new Download(intval($_GET['id']));	
		
		//Check for supersedence
		if (!$download->Superseded)
		{
			$download->InitiateDownload();
			exit;
		}

		$error = 'The requested download has been superseded with a newer version.';
	}
	catch (Exception $e)
	{
		$error = $e->getMessage();
	}
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
				<?php if (!empty($error)) printf('<p class="error">%s</p>', $error); ?>
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
						<th>Downloads</th>
					</tr>
					<tr>
						<td><a href="http://sourceforge.net/projects/eraser/files/Eraser%206/6.0.8/Eraser%206.0.8.2273.exe/download">Eraser 6.0.8.2273</a></td>
						<td>6.0.8.2273</td>
						<td>6/11/2010 9:30am</td>
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
					<tr>
						<td colspan="4">No beta builds available.</td>
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
					foreach (BuildBranch::Get() as $branch)
					{
?>
					<tr>
						<td colspan="4"><h4><?php echo $branch->Title; ?></h4></td>
					</tr>
<?php
						foreach (Build::GetActive($branch->ID) as $build)
						{
?>
					<tr>
						<td><a href="<?php echo $_SERVER['PHP_SELF'] . '?id=' . $build->ID; ?>"><?php echo $build->Name; ?></a></td>
						<td>r<?php echo $build->Revision; ?></td>
						<td><?php echo $build->Released->format('j/n/y g:ia'); ?></td>
						<td><?php echo $build->Downloads; ?></td>
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
					<a href="index.php"><img src="images/btn_home.gif" id="nav1" alt="Home" onmouseover="MM_swapImage('nav1','','images/btn_home_hov.gif',1)" onmouseout="MM_swapImgRestore()" /></a><a href="download.php"><img src="images/btn_download.gif" id="nav2" alt="Download" onmouseover="MM_swapImage('nav2', '', 'images/btn_download_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a><a href="http://bbs.heidi.ie/viewforum.php?f=30" target="_blank"><img src="images/btn_forum.gif" id="nav3" alt="Forum" onmouseover="MM_swapImage('nav3', '', 'images/btn_forum_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a><a href="http://trac.heidi.ie/"><img src="images/btn_trac.gif" id="nav4" alt="Trac" onmouseover="MM_swapImage('nav4', '', 'images/btn_trac_hov.gif', 1)" onmouseout="MM_swapImgRestore()" /></a>
				</div>
			</div>
			
			<div class="right_news">
				<h3>Latest News</h3>
				<div class="right_news_bg">
					<h2>Eraser 6.0.9 released!</h2>
					<div class="posted">Posted by: Joel, 6<sup>th</sup> November 2011, 1:30 pm +800GMT</div>
					<p>It has been one year since the last release of Eraser, and we are happy to announce the release of Eraser 6.0.9. Eraser 6.0.9 only contains bug fixes and is the most stable release of the Eraser 6.0 series; users are encouraged to upgrade to this version as soon as possible. <a href="announcements/20111106.html">Read the full announcement.</a></p>
				</div>
			</div>

			<!-- InstanceBeginRepeat name="RightContent" --><!-- InstanceEndRepeat -->
		</div>
	</div>
	<div id="footer">
		<div class="footer">
			<div class="footer_l">
				<p>2008-2010 &copy; Eraser</p>
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