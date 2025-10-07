using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WinRotor.Models.Clients;

public class WinSpecClient : IDisposable
{
    private readonly string _winSpecServerIp;
    private readonly int _winSpecServerPort;
    private TcpClient _tcpClient = null!;
    private NetworkStream _networkStream = null!;

    public WinSpecClient(string serverIp, int serverPort)
    {
        // Set server ip and port to the passed in parameters
        _winSpecServerIp = serverIp;
        _winSpecServerPort = serverPort;
    }
    public async Task Connect(CancellationToken token = default)
    {
        // Initialise a new tcp client
        _tcpClient = new TcpClient();
        
        // Connect to the client
        await _tcpClient.ConnectAsync(_winSpecServerIp, _winSpecServerPort, token);
        _networkStream = _tcpClient.GetStream();
    }

    public async Task Disconnect()
    {
        await _networkStream.DisposeAsync();
        _networkStream = null!;
        _tcpClient.Dispose();
        _tcpClient = null!;
    }
    public async void Dispose()
    {
        await Disconnect().ConfigureAwait(false);
    }

    private async Task<string> SendCommand(string command, string optionalParameter = "", CancellationToken token = default)
    {
        if (!_tcpClient.Connected) throw new Exception("Not connected");

        // Initialize a string to hold the full command
        string fullCommand = command; 
        
        // Concatenate the passed in command with optional parameter to get fullCommand
        if (!string.IsNullOrEmpty(optionalParameter)) fullCommand += "::" + optionalParameter;
        
        // Prepare the fullCommand by converting it to ASCII
        byte[] commandBytes = Encoding.ASCII.GetBytes(fullCommand); 
        
        // Send the command
        await _networkStream.WriteAsync(commandBytes, 0, commandBytes.Length, token); 
        
        // Ensure all buffered data is immediately sent
        await _networkStream.FlushAsync(token);
        
        // Initialize a byte array to store the response
        byte[] responseBytes = new byte[1024]; 
        
        // Read and store the response
        var bytesRead = await _networkStream.ReadAsync(responseBytes, 0, responseBytes.Length, token);
        
        // Convert response to string
        var response =  Encoding.ASCII.GetString(responseBytes, 0, bytesRead);
        
        return response;
    }

    public async Task<string> SendWinSpecCommand(string command, string optionalParameter = "")
    {
        try
        {
            var response = await SendCommand(command, optionalParameter);
            return response;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}