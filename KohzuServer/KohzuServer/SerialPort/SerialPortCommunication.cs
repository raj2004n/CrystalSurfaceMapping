using System.IO.Ports;
using System.Text;

namespace KohzuServer.SerialPort;

public class SerialPortCommunication : IDisposable
{
    private readonly System.IO.Ports.SerialPort _serialPort;
    
    // Initialize serial port parameters
    private const string PortName = "COM4";
    private const int BaudRate = 115200;

    public SerialPortCommunication()
    {
        _serialPort = new System.IO.Ports.SerialPort(PortName, BaudRate, Parity.None, 8, StopBits.One)
        {
            // l=Line terminator required by Kohzu
            NewLine = "\r\n", 
            
            // Initialize read and write timeout
            ReadTimeout = 5000,
            WriteTimeout = 5000,
        };
        
        // Start connection
        _serialPort.Open();
    }
    
    public async Task<string> SendCommand(string command)
    {
        // Discard leftover data
        _serialPort.DiscardInBuffer(); 
        _serialPort.DiscardOutBuffer(); 

        // Convert commands to ASCII (to make it readable for Kohzu)
        byte[] cmdBytes = Encoding.ASCII.GetBytes(command); 
        
        // Format: STX + command + CR + LF, required format by the rotors
        byte[] framed = new byte[cmdBytes.Length + 3]; 

        // STX - Start of Text byte
        framed[0] = 0x02;
        
        // Add command after STX
        Array.Copy(cmdBytes, 0, framed, 1, cmdBytes.Length);
        
        // CR - Carriage return byte, signals return to start of line
        framed[^2] = 0x0D;
        
        // LF - Line Feed, signals to go to next line vertically down
        framed[^1] = 0x0A;
        
        try
        {
            // Send the command
            await Task.Run(() => _serialPort.Write(framed, 0, framed.Length));
            
            // Receive feedback from rotors
            var response = await Task.Run(() => _serialPort.ReadLine()); 
            
            return response;
        }
        catch (TimeoutException)
        {
            Console.WriteLine("[TIMEOUT] No response received.");
            throw; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Sending command {command}: {ex.Message}");
            throw;
        }
    }
    
    public void Dispose()
    {
        if (_serialPort.IsOpen)
        {
            // Close the serial port
            _serialPort.Close(); 
            
            // Release resources
            _serialPort.Dispose();
        }
        
        // Prevent the finalizer from running
        GC.SuppressFinalize(this);
    }
}