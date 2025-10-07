using Grpc.Core;
using KohzuServer.SerialPort;

namespace KohzuServer.Services;

public class RotorStatusService : RotorStatus.RotorStatusBase
{
    private readonly ILogger<RotorStatusService> _logger;
    private readonly SerialPortCommunication _serialPortCommunication;

    public RotorStatusService(ILogger<RotorStatusService> logger, SerialPortCommunication serialPortCommunication)
    {
        _logger = logger;
        _serialPortCommunication =  serialPortCommunication;
    }

    public override async Task<IdnResponse> TestConnection(IdnRequest request, ServerCallContext context)
    {
        // Initially set the response to false => not connected
        IdnResponse response = new IdnResponse
        {
            Result = false
        };
        
        try
        {
            // Try to send a command
            await _serialPortCommunication.SendCommand("");
            // If past the above line then rotor is properly connected
            response.Result = true;
            return await Task.FromResult(response);
        }
        catch (TimeoutException)
        {
            // If not connected then will time out
            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            // If some other error occurs
            // Highly unlikely, this would indicate that the rotors are not properly connected to the Windows 10
            Console.WriteLine($"Faced an unexpected error: {ex.Message}");
            return await Task.FromResult(response);
        }
    }

    public override async Task<RdeResponse> GetEncoderPosition(RdeRequest request, ServerCallContext context)
    {
        RdeResponse response = new RdeResponse();
        
        // Initialize command
        var command = $"RDE{request.Axis}";
        
        // Send command
        var feedback = await _serialPortCommunication.SendCommand(command);
        
        // Pick out position from feedback
        // e.g. of response.Position: C	RDE1	-8750
        response.Position = feedback.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
        
        return await Task.FromResult(response);
    }
    
    public override async Task<RdpResponse> GetPresentPosition(RdpRequest request, ServerCallContext context)
    {
        RdpResponse response = new RdpResponse();
        
        // Initialize command
        var command = $"RDP{request.Axis}";
        
        // Send command
        var feedback = await _serialPortCommunication.SendCommand(command);
        
        // Pick out position from feedback
        response.Position = feedback.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
        
        return await Task.FromResult(response);
    }
    
}