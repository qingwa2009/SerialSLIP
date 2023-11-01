/*
 * Created by SharpDevelop.
 * User: qingw
 * Date: 2023/6/30
 * Time: 22:47
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.IO.Ports;
	
namespace SerialSLIP
{
	/// <summary>
	/// Description of SerialSLIP.
	/// </summary>
	public class SerialSLIP : SerialPort
	{
		const int SERIAL_RX_BUFFER_SIZE = 64;
		public delegate void PacketRecvHandler(byte[] data, int len, bool isPassCheckSum);

		public event PacketRecvHandler OnRecvPacket;
		
		public SerialSLIP(string PortName, int baudRate):base(PortName, baudRate)
		{
			this.DataReceived += new SerialDataReceivedEventHandler(SerialSLIP_DataReceived);
		}
		enum State
		{
			unknown = 0,		//此时收到1个0xc0会waitForStart
			waitForStart = 1, //此时需要收到1个0xc0才会进入接收状态，否则进入unknown
			waitForEnd = 2,	//此时收到1个0xc0会进入waitForStart
		};
		
		State _state = State.waitForStart;
		byte[] _recvbuf=new byte[SERIAL_RX_BUFFER_SIZE];
		byte _recvbufInd = 0;
		
		bool _nextcharEscape = true;
		byte _sum = 0;
		
		void SerialSLIP_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			 
			int r;
			while ((r = this.ReadByte()) >= 0)
			{
				byte c = (byte)r;
				//未知状态
				if (_state == State.unknown)
				{
					if (c == 0xC0)
						_state = State.waitForStart;
					continue;
				}
		
				//等待开始状态
				if (_state == State.waitForStart)
				{
					_state = c == 0xC0 ? State.waitForEnd : State.unknown;
					continue;
				}
		
				//正常接收状态
				byte i = _recvbufInd;
		
				if (c == 0xC0)
				{
					if(OnRecvPacket!=null)
						OnRecvPacket(_recvbuf, i - 1, _sum == _recvbuf[i - 1]); //接收到一个完整包
					_recvbufInd = 0;
					_nextcharEscape = true;
					_sum = 0;
					_state = State.waitForStart;
					return;
				}
		
				if (i > 0)
				{
					if (_recvbuf[i - 1] == 0xDB && _nextcharEscape == true)
					{
						if (c == 0xDC)
						{
							_recvbuf[i - 1] = 0xC0;
							continue;
						}
						if (c == 0xDD)
						{
							// _recvbuf[i - 1] = 0xDB;
							_nextcharEscape = false;
							continue;
						}
					}
					_nextcharEscape = true;
					unchecked {
						_sum += _recvbuf[i - 1];
					}
				}
		
				_recvbuf[i] = c;
				if (i < SERIAL_RX_BUFFER_SIZE - 1)
					_recvbufInd++;
			}
		}
		
		public void SendPacket(byte[] data)
		{
			using (var ms=new MemoryStream(SERIAL_RX_BUFFER_SIZE*2+2)) {
				using (var bs=new BinaryWriter(ms)) {
					bs.Write((byte)0xC0);
					byte sum = 0;
					for (int i = 0; i < data.Length; i++)
					{
						byte d = data[i];
						unchecked{
							sum += d;
						} 
						if (d == 0xC0)
						{
							bs.Write((byte)0xDB);
							bs.Write((byte)0xDC);
						}
						else if (d == 0xDB)
						{
							bs.Write((byte)0xDB);
							bs.Write((byte)0xDD);
						}
						else
						{
							bs.Write((byte)d);
						}
					}
					//校验和
					if (sum == 0xC0)
					{
						bs.Write((byte)0xDB);
						bs.Write((byte)0xDC);
					}
					else if (sum == 0xDB)
					{
						bs.Write((byte)0xDB);
						bs.Write((byte)0xDD);
					}
					else
					{
						bs.Write((byte)sum);
					}
					bs.Write((byte)0xC0);
				}
				var buf = ms.ToArray();
				this.Write(buf,0,buf.Length);
			}	
		}
	}
}
