using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class ThinkGearConnector : MonoBehaviour
{
    public SerialPort stream;
    public int serialID = 0;

    const int MAX_PACKET_LENGTH = 169;
    const int EEG_POWER_BANDS = 8;

    byte latestByte;
    byte lastByte;
    bool inPacket = false;
    byte packetIndex;
    byte packetLength;
    byte checksum;
    byte checksumAccumulator;

    public List<byte> packetData = new List<byte>();

    public byte signalQuality;
    public byte attention;
    public byte meditation;
    public byte blinkStrength;
    public short rawValue;
    public int[] eegPower;
    public List<int> rawValues = new List<int>();



    void Start()
    {
        eegPower = new int[EEG_POWER_BANDS];

        if (SerialPort.GetPortNames().Length > 0)
        {
            foreach (var port in SerialPort.GetPortNames())
            {
                Debug.Log(port);
            }
            Debug.Log("Opening port: " + SerialPort.GetPortNames()[serialID]);
            stream = new SerialPort(SerialPort.GetPortNames()[serialID], 9600);
            stream.ReadTimeout = 50;
            stream.Open();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!stream.IsOpen) return;
        while (stream.BytesToRead > 0)
        {
            latestByte = (byte)stream.ReadByte();
            Debug.Log("Getting information");

            if (inPacket)
            {
                // First byte after the sync bytes is the length of the upcoming packet.
                if (packetIndex == 0)
                {
                    packetLength = latestByte;

                    // Catch error if packet is too long
                    if (packetLength > MAX_PACKET_LENGTH)
                    {
                        // Packet exceeded max length
                        // Send an error
                        Debug.LogError("ERROR: Packet too long " + packetLength);
                        inPacket = false;
                    }
                }
                else if (packetIndex <= packetLength)
                {
                    // Run of the mill data bytes.

                    // Print them here

                    // Store the byte in an array for parsing later.
                    packetData.Add(latestByte);

                    // Keep building the checksum.
                    checksumAccumulator += latestByte;
                }
                else if (packetIndex > packetLength)
                {
                    // We're at the end of the data payload.

                    // Check the checksum.
                    checksum = latestByte;
                    checksumAccumulator = (byte)(255 - checksumAccumulator);

                    // Do they match?
                    if (checksum == checksumAccumulator)
                    {
                        Debug.Log("Checksum correct!");
                        Debug.Log(packetLength + " Bytes received");

                        bool parseSuccess = ParsePacket();

                        if (parseSuccess)
                        {
                            Debug.Log("Parsing success!");
                        }
                        else
                        {
                            // Parsing failed, send an error.
                            Debug.LogError("ERROR: Could not parse");
                            // good place to print the packet if debugging
                        }
                    }
                    else
                    {
                        // Checksum mismatch, send an error.
                        Debug.LogError("ERROR: Checksum");
                        // good place to print the packet if debugging
                    }
                    // End of packet

                    // Reset, prep for next packet
                    inPacket = false;
                }

                packetIndex++;
            }


            if ((latestByte == 170) && (lastByte == 170) && !inPacket)
            {
                // Start of packet
                inPacket = true;
                packetData.Clear();
                packetIndex = 0;
                checksumAccumulator = 0;
            }

            // Keep track of the last byte so we can find the sync byte pairs.
            lastByte = latestByte;
        }
    }


    bool ParsePacket()
    {
        // Loop through the packet, extracting data.
        // Based on mindset_communications_protocol.pdf from the Neurosky Mindset SDK.
        // Returns true if passing succeeds
        bool parseSuccess = true;


        for (int i = 0; i < packetLength; i++)
        {
            switch (packetData[i])
            {
                case 0x2:
                    signalQuality = packetData[++i];
                    break;
                case 0x4:
                    attention = packetData[++i];
                    break;
                case 0x5:
                    meditation = packetData[++i];
                    break;
                case 0x16:
                    blinkStrength = packetData[++i];
                    break;
                case 0x83:
                    // ASIC_EEG_POWER: eight big-endian 3-uint8_t unsigned integer values representing delta, theta, low-alpha high-alpha, low-beta, high-beta, low-gamma, and mid-gamma EEG band power values
                    // The next uint8_t sets the length, usually 24 (Eight 24-bit numbers... big endian?)
                    // We dont' use this value so let's skip it and just increment i
                    i++;

                    // Extract the values
                    for (int j = 0; j < EEG_POWER_BANDS; j++)
                    {
                        eegPower[j] = (packetData[++i] << 16) | (packetData[++i] << 8) | packetData[++i];
                    }

                    break;
                case 0x80:
                    // We dont' use this value so let's skip it and just increment i
                    // uint8_t packetLength = packetData[++i];
                    i++;
                    rawValue = (short)((packetData[++i] << 8) | packetData[++i]);
                    rawValues.Add(rawValue);
                    if (rawValues.Count > 500)
                        rawValues.RemoveAt(0);
                    break;
                default:
                    // Broken packet ?
                    /*
                    Serial.print(F("parsePacket UNMATCHED data 0x"));
                    Serial.print(packetData[i], HEX);
                    Serial.print(F(" in position "));
                    Serial.print(i, DEC);
                    printPacket();
                    */
                    parseSuccess = false;
                    break;
            }
        }
        return parseSuccess;
    }

    private void OnApplicationQuit()
    {
        stream.Close();
    }
}
