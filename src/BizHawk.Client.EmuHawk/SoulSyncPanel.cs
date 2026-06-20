#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Panneau latéral SoulSync : héberge une WebView2 qui affiche le dashboard
	/// HTML/CSS. Conçu pour être docké (DockStyle.Right) dans la fenêtre EmuHawk
	/// = une seule fenêtre. La WebView2 s'initialise paresseusement au 1er affichage.
	/// Plus tard : alimenté en direct par le C# (lecture mémoire → PostWebMessage).
	/// </summary>
	public sealed class SoulSyncPanel : UserControl
	{
		private readonly WebView2 _web;
		private bool _initStarted;

		public SoulSyncPanel()
		{
			Width = 430;
			BackColor = System.Drawing.Color.FromArgb(10, 19, 34);
			_web = new WebView2 { Dock = DockStyle.Fill };
			Controls.Add(_web);
		}

		/// <summary>Initialise la WebView2 au premier affichage (évite de la créer si jamais ouverte).</summary>
		public void EnsureStarted()
		{
			if (_initStarted) return;
			_initStarted = true;
			_ = InitAsync();
		}

		private async Task InitAsync()
		{
			try
			{
				var userDataFolder = Path.Combine(Path.GetTempPath(), "SoulSyncWebView2");
				var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
				await _web.EnsureCoreWebView2Async(env);

				var uiDir = ResolveUiDir();
				if (uiDir is not null)
				{
					// Sert le dossier ui/ via un hôte virtuel : la WebView charge le vrai dashboard.
					_web.CoreWebView2.SetVirtualHostNameToFolderMapping(
						"soulsync.ui", uiDir, CoreWebView2HostResourceAccessKind.Allow);
					_web.CoreWebView2.Navigate("https://soulsync.ui/index.html");
				}
				else
				{
					_web.CoreWebView2.NavigateToString(PlaceholderHtml); // repli
				}
			}
			catch (Exception ex)
			{
				_initStarted = false; // autorise un nouvel essai
				Controls.Add(new Label
				{
					Dock = DockStyle.Top,
					AutoSize = false,
					Height = 90,
					ForeColor = System.Drawing.Color.White,
					Text = "WebView2 indisponible : " + ex.Message
						+ "\nInstalle le runtime Microsoft Edge WebView2.",
				});
			}
		}

		/// <summary>Locates the repo's ui/ folder relative to the exe (dev or packaged).</summary>
		private static string? ResolveUiDir()
		{
			var baseDir = AppContext.BaseDirectory;
			foreach (var rel in new[] { "ui", "../ui", "../../ui", "../../../ui" })
			{
				var p = Path.GetFullPath(Path.Combine(baseDir, rel));
				if (File.Exists(Path.Combine(p, "dashboard.html"))) return p;
			}
			return null;
		}

		private const string PlaceholderHtml = @"<!doctype html>
<html lang='fr'><head><meta charset='utf-8'>
<style>
  :root { --soul:#ff5470; --sync:#36d1dc; --or:#f5c451; }
  * { box-sizing:border-box; }
  body { margin:0; font-family:'Segoe UI',sans-serif; color:#eef0f6;
         background:linear-gradient(160deg,#0a1322,#16294a); min-height:100vh; }
  header { padding:18px 16px; text-align:center;
           background:linear-gradient(90deg,var(--soul),var(--sync)); }
  header h1 { margin:0; font-size:22px; letter-spacing:1px; }
  header p { margin:4px 0 0; opacity:.85; font-size:12px; }
  .cards { padding:14px; display:flex; flex-direction:column; gap:10px; }
  .card { background:rgba(255,255,255,.06); border:1px solid rgba(255,255,255,.12);
          border-left:5px solid var(--sync); border-radius:10px; padding:12px;
          box-shadow:0 4px 14px rgba(0,0,0,.4); display:flex; align-items:center; gap:12px; }
  .card .pic { width:56px; height:56px; border-radius:50%;
               background:radial-gradient(circle at 40% 35%, #fff3, #0003); }
  .card .lvl { font-size:12px; opacity:.8; }
  .hp { height:9px; border-radius:5px; background:#0006; margin-top:6px; overflow:hidden; }
  .hp > i { display:block; height:100%; background:#5ad94e; }
  .link { color:var(--or); font-weight:bold; font-size:11px; }
  footer { text-align:center; font-size:11px; opacity:.6; padding:10px; }
</style></head>
<body>
  <header><h1>SoulSync</h1><p>Panneau docké dans l'émulateur ✔</p></header>
  <div class='cards'>
    <div class='card'><div class='pic'></div>
      <div style='flex:1'><b>Salamèche</b> <span class='link'>🔗 lien #1</span>
        <div class='lvl'>Niv. 12</div><div class='hp'><i style='width:80%'></i></div></div></div>
    <div class='card' style='border-left-color:var(--soul)'><div class='pic'></div>
      <div style='flex:1'><b>Carchacrok</b> <span class='link'>🔗 lien #2</span>
        <div class='lvl'>Niv. 14</div><div class='hp'><i style='width:55%;background:#ffd23f'></i></div></div></div>
  </div>
  <footer>Maquette — bientôt alimenté en direct par la RAM du jeu</footer>
</body></html>";
	}
}
