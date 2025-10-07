using Grpc.Core;
using KohzuServer.SerialPort;

namespace KohzuServer.Services;

public class RotorDriveService : RotorDrive.RotorDriveBase
{
    private readonly ILogger<RotorStatusService> _logger;
    private readonly SerialPortCommunication _serialPortCommunication;
    
    private const int X = 3;
    private const int Y = 2;
    private const int Z = 1;
    
    private const int ResponseMethod = 0; // 0 to ensure rotors execute next command after completing prior one
    private const int Speed = 4;

    public RotorDriveService(ILogger<RotorStatusService> logger, SerialPortCommunication serialPortCommunication)
    {
        _logger = logger;
        _serialPortCommunication = serialPortCommunication;
    }
    
    public override async Task<RpsResponse> Rps(RpsRequest request, ServerCallContext context)
    {
        RpsResponse response = new();
        
        // Initialize command to execute movement
        var command = $"RPS{request.Axis}/{Speed}/{request.StepSize}/{ResponseMethod}";
        
        // Send Command
        var feedback = await _serialPortCommunication.SendCommand(command);
        
        // Assign feedback
        response.Feedback = feedback;
        return await Task.FromResult(response);
    }
    
    public override async Task<ApsResponse> Aps(ApsRequest request, ServerCallContext context)
    {
        ApsResponse response = new();

        // Initialize commands to execute movement
        var commandX = $"APS{X}/{Speed}/{request.PosX}/{ResponseMethod}";
        var commandY = $"APS{Y}/{Speed}/{request.PosY}/{ResponseMethod}";
        var commandZ = $"APS{Z}/{Speed}/{request.PosZ}/{ResponseMethod}";

        // Send commands and assign their feedbacks
        response.Feedback1 = await _serialPortCommunication.SendCommand(commandX);
        response.Feedback2 = await _serialPortCommunication.SendCommand(commandY);
        response.Feedback3 = await _serialPortCommunication.SendCommand(commandZ);
        
        return await Task.FromResult(response);
    }

    public override async Task<Mps2Response> Mps2(Mps2Request request, ServerCallContext context)
    {
        Mps2Response response = new();
        
        // Diving type: if 0 => Absolute Position Drive, 1 => Relative Position Drive
        var drivingType = request.DrivingType; 
        
        // Preparation commands - these set speed and driving type
        var commandPrepX = $"MPI{X}/{X}/{drivingType}/{Speed}";
        var commandPrepY = $"MPI{Y}/{Y}/{drivingType}/{Speed}";
        
        // Command to execute the movement
        var commandMove = $"MPS{X}/{request.PosX}/{Y}/{request.PosY}/{ResponseMethod}";
        
        // Send the preparation commands
        await _serialPortCommunication.SendCommand(commandPrepX);
        await _serialPortCommunication.SendCommand(commandPrepY);
        
        // Send the movement execution command assign the feedback
        response.Feedback = await _serialPortCommunication.SendCommand(commandMove);
        
        return await Task.FromResult(response);
    }
    
    public override async Task<Mps3Response> Mps3(Mps3Request request, ServerCallContext context)
    {
        Mps3Response response = new();

        // Diving type: if 0 => Absolute Position Drive, 1 => Relative Position Drive
        var drivingType = request.DrivingType; 
        
        // Preparation commands - these set speed and driving type
        var commandPrepX = $"MPI{X}/{X}/{drivingType}/{Speed}";
        var commandPrepY = $"MPI{Y}/{Y}/{drivingType}/{Speed}";
        var commandPrepZ = $"MPI{Z}/{Z}/{drivingType}/{Speed}";
        
        // Command to execute the movement
        var commandMove = $"MPS{X}/{request.PosX}/{Y}/{request.PosY}/{Z}/{request.PosZ}/{ResponseMethod}";
        
        // Send the preparation commands
        await _serialPortCommunication.SendCommand(commandPrepX);
        await _serialPortCommunication.SendCommand(commandPrepY);
        await _serialPortCommunication.SendCommand(commandPrepZ);
        
        // Send the movement execution command assign the feedback
        response.Feedback = await _serialPortCommunication.SendCommand(commandMove);
        
        return await Task.FromResult(response);
    } 
    

}