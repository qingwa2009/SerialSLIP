/*
 * Created by SharpDevelop.
 * User: qingw
 * Date: 2023/6/30
 * Time: 22:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace SerialSLIP
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			
			// TODO: Implement Functionality Here
			SerialSLIP sp = new SerialSLIP("COM3",250000);
			sp.OnRecvPacket += new SerialSLIP.PacketRecvHandler(sp_OnRecvPacket);
			sp.Open();
			
			Console.Write("Press any key to continue . . . ");
			while (true) {
				string s = Console.ReadLine();
				var bs=System.Text.Encoding.Default.GetBytes(s);
				sp.SendPacket(bs);
			}
			
		}

		static void sp_OnRecvPacket(byte[] data, int len, bool isPassCheckSum)
		{
			if(isPassCheckSum){
           		var s=System.Text.Encoding.Default.GetString(data, 0, len);
           		Console.WriteLine(s);
           	}else{
           		Console.WriteLine("pc check sum error!");
           	}
		}
	}
}