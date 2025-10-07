using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using KohzuServer;
using WinRotor.Common;

namespace WinRotor.Models.Clients;

public class RotorClient
{
    private readonly GrpcChannel _rotorChannel;
    private readonly RotorDrive.RotorDriveClient _rotorDrive;
    private readonly RotorStatus.RotorStatusClient _rotorStatus;
    public RotorClient(string rotorServerIp, int rotorServerPort)
    {
        _rotorChannel = GrpcChannel.ForAddress("http://" + rotorServerIp + ":" + rotorServerPort); // initialize channel
        _rotorDrive =  new RotorDrive.RotorDriveClient(_rotorChannel);
        _rotorStatus = new  RotorStatus.RotorStatusClient(_rotorChannel);
    }

    private string CheckFeedback(string feedback)
    {
        var feedbackType = feedback.Substring(0, 1);
        string message = "Ok";
        switch (feedbackType)
        {
            case "C":
                break;
            case "W":
                message = "Failed to execute a Rotor Command.\r\n" +
                          $"Warning: {feedback}\r\n" +
                          $"Please check the error code against the 'KOSMOS series Model: ARIES/LYNX Manual.";
                break;
            case "E":
                message = "Failed to execute a Rotor Command.\r\n" +
                          $"Error: {feedback}\r\n" +
                          $"Please check the error code against the 'KOSMOS series Model: ARIES/LYNX Manual.";
                break;
        }

        return message;
    }

    public async Task<bool> TestConnection()
    {
        // Use a read status command to test if connected
        var request = new IdnRequest();
        // response.Result is true or false
        var response = await _rotorStatus.TestConnectionAsync(request);
        return response.Result;
    }

    public async Task<(string, string, string)> GetStatus()
    {
        var request = new StrRequest
        {
            AxisX = GlobalParameters.X,
            AxisY = GlobalParameters.Y,
            AxisZ = GlobalParameters.Z
        };
        
        var response = await _rotorStatus.GetStatusAsync(request);
        var statusX = CheckFeedback(response.FeedbackX);
        var statusY = CheckFeedback(response.FeedbackY);
        var statusZ = CheckFeedback(response.FeedbackZ);
        
        return (statusX, statusY,  statusZ);
    }
    
    public async Task<string> Rps(int axis, double stepSize)
    {
        // Convert stepsize from micron to pulses
        if (axis == GlobalParameters.Y) 
        {
            stepSize *= GlobalParameters.PulseOverMicronsY;
        }
        else if (axis == GlobalParameters.Z) 
        {
            stepSize *= -GlobalParameters.PulseOverMicronsZ;
        }
        
        // Initialise request
        // Note: Step size in pulses must be integer
        var request = new RpsRequest
        {
            Axis = axis,
            StepSize = (int)Math.Round(stepSize),
        };
        
        var response = await _rotorDrive.RpsAsync(request);
        return CheckFeedback(response.Feedback);
    }

    public async Task<int> Rde(int axis)
    {
        var request = new RdeRequest
        {
            Axis = axis
        };
        var response = await _rotorStatus.GetEncoderPositionAsync(request);
        var position = Int32.Parse(response.Position);
        return position;
    }

    public async Task<int> Rdp(int axis)
    {
        var request = new RdpRequest
        {
            Axis = axis
        };
        var response = await _rotorStatus.GetPresentPositionAsync(request);
        var position = Int32.Parse(response.Position);
        return position;
    }

    public async Task<(int xPos, int yPos, int zPos)> GetXyz()
    {
        // return x, y, z present position
        return (await Rdp(GlobalParameters.X), await Rdp(GlobalParameters.Y), await Rdp(GlobalParameters.Z));
    }
    
    public async Task<(int xPosE, int yPosE, int zPosE)> GetXyzE()
    {
        // return x, y, z present position
        return (await Rde(GlobalParameters.X), await Rde(GlobalParameters.Y), await Rde(GlobalParameters.Z));
    }
    
    public async Task<string> Mps3(double posX, double posY, double posZ, int drivingType)
    {
        // Initialise variable to hold integer value of position in pulses
        
        // If relative position drive, then input is in microns
        if (drivingType == 1)
        {
            posY *= GlobalParameters.PulseOverMicronsY;
            posZ *= -GlobalParameters.PulseOverMicronsZ;
        }
        var request = new Mps3Request
        {
            PosX = (int)Math.Round(posX),
            PosY = (int)Math.Round(posY),
            PosZ = (int)Math.Round(posZ),
            DrivingType = drivingType // 0: Absolute, 1: Relative
        };
        var response = await _rotorDrive.Mps3Async(request);
        return CheckFeedback(response.Feedback);
    }
    
    public async Task<string> Mps2(double posY, double posZ, int drivingType)
    {
        // If relative position drive, then convert from microns to pulses
        if (drivingType == 1)
        {
            posY *= GlobalParameters.PulseOverMicronsY;
            posZ *= -GlobalParameters.PulseOverMicronsZ;
        }
        
        // Convert stepsize from micron to pulses
        var request = new Mps2Request
        {
            PosY = (int)Math.Round(posY),
            PosZ = (int)Math.Round(posZ),
            DrivingType = drivingType // 0: Absolute, 1: Relative
        };
        var response = await _rotorDrive.Mps2Async(request);
        return CheckFeedback(response.Feedback);
    }
}