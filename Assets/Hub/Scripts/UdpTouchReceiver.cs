using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpTouchReceiver : MonoBehaviour
{
    [Header("UDP/OSC")]
    public int listenPort = 9000;

    [Header("Output (read from other scripts)")]
    public Vector2 latest01 = new Vector2(0.5f, 0.5f); // 0..1
    public bool isDown = false;
    public int phase = 0; // 0=Up, 1=Down, 2=Move
    public int id = 0;
    public string lastRaw = "";

    // phase変化ログ用
    private int _prevPhase = int.MinValue;

    private UdpClient _udp;
    private Thread _thread;
    private volatile bool _running;

    // -------- thread -> main sync --------
    private readonly object _sync = new object();
    private bool _hasPending = false;
    private int _pId;
    private float _pX01;
    private float _pY01;
    private int _pPhase;

    void Start()
    {
        // Any: どのNICでも受ける（ローカルなら127.0.0.1でも届く）
        _udp = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));
        _udp.Client.ReceiveTimeout = 1000;

        _running = true;
        _thread = new Thread(RecvLoop) { IsBackground = true };
        _thread.Start();

        Debug.Log($"[UdpTouchReceiver] Listening OSC UDP {listenPort}...");
    }

    void OnDestroy()
    {
        _running = false;

        try { _udp?.Close(); } catch { }
        _udp = null;

        try { _thread?.Join(300); } catch { }
        _thread = null;
    }

    void Update()
    {
        // スレッドからの受信値を反映（Unityの変数はメインスレッドで触る）
        if (_hasPending)
        {
            int rid, rph;
            float rx, ry;

            lock (_sync)
            {
                rid = _pId;
                rx = _pX01;
                ry = _pY01;
                rph = _pPhase;
                _hasPending = false;
            }

            id = rid;
            latest01 = new Vector2(rx, ry);
            phase = rph;
            isDown = (phase == 1 || phase == 2);

            lastRaw = $"/touch id={id} x={latest01.x:0.0000} y={latest01.y:0.0000} phase={phase}";
        }

        // phaseが変わった時だけログ
        if (phase != _prevPhase)
        {
            _prevPhase = phase;
            Debug.Log($"[OSC RECV] {lastRaw} isDown={isDown}");
        }

        // （任意）このGameObjectを動かしたいなら
        // transform.position = new Vector3(latest01.x * 10f, latest01.y * 10f, 0f);
    }

    private void RecvLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

        while (_running)
        {
            try
            {
                var bytes = _udp.Receive(ref ep);

                if (TryParseOscTouch(bytes, out int rid, out float x01, out float y01, out int rph))
                {
                    x01 = Mathf.Clamp01(x01);
                    y01 = Mathf.Clamp01(y01);

                    lock (_sync)
                    {
                        _pId = rid;
                        _pX01 = x01;
                        _pY01 = y01;
                        _pPhase = rph;
                        _hasPending = true;
                    }
                }
            }
            catch (SocketException)
            {
                // timeout想定
            }
            catch (Exception)
            {
                // 受信中の例外は握りつぶし（必要ならログ）
            }
        }
    }

    // ---------------- OSC minimal parser ----------------
    // Expect: address "/touch", types ",iffi"
    // args: id(int), x(float), y(float), phase(int)
    private static bool TryParseOscTouch(byte[] data, out int id, out float x, out float y, out int phase)
    {
        id = 0; x = y = 0; phase = 0;

        try
        {
            int i = 0;

            string addr = ReadPaddedString(data, ref i);
            if (addr != "/touch") return false;

            string types = ReadPaddedString(data, ref i);
            if (types != ",iffi") return false;

            id = ReadIntBE(data, ref i);
            x = ReadFloatBE(data, ref i);
            y = ReadFloatBE(data, ref i);
            phase = ReadIntBE(data, ref i);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadPaddedString(byte[] data, ref int i)
    {
        int start = i;
        while (i < data.Length && data[i] != 0) i++;
        if (i >= data.Length) throw new Exception("OSC string not terminated");

        string s = Encoding.ASCII.GetString(data, start, i - start);
        i++; // null
        while ((i % 4) != 0) i++; // pad
        return s;
    }

    private static int ReadIntBE(byte[] data, ref int i)
    {
        if (i + 4 > data.Length) throw new Exception("OSC int overflow");
        int v = (data[i] << 24) | (data[i + 1] << 16) | (data[i + 2] << 8) | data[i + 3];
        i += 4;
        return v;
    }

    private static float ReadFloatBE(byte[] data, ref int i)
    {
        if (i + 4 > data.Length) throw new Exception("OSC float overflow");
        byte[] b = new byte[4];
        b[0] = data[i]; b[1] = data[i + 1]; b[2] = data[i + 2]; b[3] = data[i + 3];
        if (BitConverter.IsLittleEndian) Array.Reverse(b);
        i += 4;
        return BitConverter.ToSingle(b, 0);
    }
}
