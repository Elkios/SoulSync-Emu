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
	/// Panneau SoulSync : héberge une WebView2 qui affiche le dashboard HTML/CSS.
	/// Étape 1 du fork : prouver l'intégration WebView2 dans EmuHawk. Le contenu
	/// est pour l'instant inline (NavigateToString) ; il sera alimenté par le C#
	/// (lecture mémoire → PostWebMessage) une fois le tracker porté.
	/// </summary>
	public sealed class SoulSyncDashboard : Form
	{
		private readonly WebView2 _web;

		public SoulSyncDashboard()
		{
			Text = "SoulSync — Dashboard";
			Width = 460;
			Height = 720;
			MinimumSize = new System.Drawing.Size(320, 400);
			ShowInTaskbar = false;
			_web = new WebView2 { Dock = DockStyle.Fill };
			Controls.Add(_web);
			Load += async (_, _) => await InitAsync();
		}

		private async Task InitAsync()
		{
			try
			{
				var userDataFolder = Path.Combine(Path.GetTempPath(), "SoulSyncWebView2");
				var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
				await _web.EnsureCoreWebView2Async(env);
				_web.CoreWebView2.NavigateToString(PlaceholderHtml);
			}
			catch (Exception ex)
			{
				var lbl = new Label
				{
					Dock = DockStyle.Fill,
					Text = "WebView2 indisponible : " + ex.Message
						+ "\n\nInstalle le runtime Microsoft Edge WebView2.",
				};
				Controls.Add(lbl);
			}
		}

		private const string PlaceholderHtml = @"<!doctype html>
<html lang='fr'><head><meta charset='utf-8'>
<style>
  :root { --soul:#ff5470; --sync:#36d1dc; --or:#f5c451; }
  * { box-sizing:border-box; }
  body { margin:0; font-family:'Segoe UI',sans-serif; color:#eef0f6;
         background:linear-gradient(160deg,#0a1322,#16294a); height:100vh; }
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
  <header><h1>SoulSync</h1><p>WebView2 intégré dans l'émulateur ✔</p></header>
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
