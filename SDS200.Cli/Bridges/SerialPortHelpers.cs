using System.IO.Ports;

namespace SDS200.Cli.Bridges;

/// <summary>
/// Utility methods for serial port discovery and scanner detection.
/// </summary>
public static class SerialPortHelpers
{
    /// <summary>
    /// Returns available serial ports, filtered to exclude debug and Bluetooth ports.
    /// Prefers cu.* (macOS call-out) over tty.*, and prioritizes usbserial/usbmodem.
    /// </summary>
    /// <returns>Filtered and prioritized array of serial port names.</returns>
    public static string[] GetFilteredPorts()
    {
        return SerialPort.GetPortNames()
            .Where(p => !p.Contains("debug", StringComparison.OrdinalIgnoreCase) &&
                        !p.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Contains("/dev/cu.", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.Contains("usbserial", StringComparison.OrdinalIgnoreCase) ||
                                    p.Contains("usbmodem", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Auto-detect the SDS200 scanner by probing each filtered port with the MDL command.
    /// Returns the port name if found, or null.
    /// </summary>
    /// <param name="log">Optional callback for logging detection progress.</param>
    /// <returns>The detected port name, or null if not found.</returns>
    public static async Task<string?> DetectScannerPortAsync(Action<string>? log = null)
    {
        var ports = GetFilteredPorts();
        if (ports.Length == 0)
        {
            log?.Invoke("No serial ports found (after filtering debug/Bluetooth).");
            return null;
        }

        log?.Invoke($"Probing ports: {string.Join(", ", ports)}");

        foreach (var port in ports)
        {
            try
            {
                log?.Invoke($"Testing {port}...");
                using var testPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500
                };
                testPort.Open();
                testPort.Write("MDL\r");
                await Task.Delay(500);

                var response = testPort.ReadExisting();
                testPort.Close();

                log?.Invoke($"Response from {port}: {(string.IsNullOrEmpty(response) ? "(empty)" : response.Replace("\r", "").Replace("\n", " "))}");

                if (response.Contains("SDS200", StringComparison.OrdinalIgnoreCase) ||
                    response.Contains("UNIDEN", StringComparison.OrdinalIgnoreCase))
                {
                    return port;
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Error testing {port}: {ex.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all available serial port names without filtering.
    /// </summary>
    /// <returns>Array of all serial port names.</returns>
    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();
}

