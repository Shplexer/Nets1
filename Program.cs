using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

class Program {
    static private List<BitArray> bitMatrix = new List<BitArray>();
    static private List<BitArray> parityControlMatrix = new List<BitArray>();
    static private List<BitArray> verticalAndHorizontalParityControlMatrix = new List<BitArray>();
    //static private BitArray 
    static private BitArray VControlSumBits = new BitArray(0);
    static private BitArray HControlSumBits = new BitArray(0);
    static private ushort poly = 0x0805;
    static void Main() {
        string filePath = "file1.txt";
        byte[] incomingMessage = ReadFile(filePath);
        foreach (byte b in incomingMessage) {
            Console.WriteLine($"0x{b:X4} : {b}");
        }
        byte[] convertedMessage = incomingMessage;
        //byte[] convertedMessage = ConvertToCorrectEncoding(incomingMessage, 1251, 866);
        bitMatrix = GetBitMatrix((byte[])convertedMessage.Clone());

        Console.WriteLine("=1===");
        foreach (byte b in convertedMessage) {
            Console.WriteLine($"0x{b:X4} : {b}");
        }
        Console.WriteLine("==1==");


        parityControlMatrix = ParityControl(bitMatrix);
        verticalAndHorizontalParityControlMatrix = VerticalAndHorizontalParityControl(bitMatrix);
        ushort crc = CalculateCRC((byte[])convertedMessage.Clone());
        //crcBits = CRC(newBytes);
        PrintBinary(crc);
        Console.WriteLine($"0x{crc:X4}");
        PrintMatrix(bitMatrix, "Исходные данные");
        PrintMatrix(parityControlMatrix, "Контроль по паритету");
        PrintMatrix(verticalAndHorizontalParityControlMatrix, "Вертикальный и горизонтальный контроль по паритету");
        
        SaveToFile(filePath);
        //SaveToFile(newBytes, filePath);
    }
    private static byte[] ReadFile(string filePath) {

        byte[] fileBytes = File.ReadAllBytes(filePath);
        return fileBytes;
    }
    private static void SaveToFile(string filePath) {
        filePath = filePath.Substring(0, filePath.Length - 4) + ".ccs";
        Console.WriteLine("Сохраняются байты: ");
        List<BitArray> bitArrays = new List<BitArray>() { VControlSumBits, HControlSumBits };
        List<String> names = ["Вертикальная контрольная сумма", "Горизонтальная контрольная сумма"];
        int index = 0;
        using (StreamWriter writer = new StreamWriter(filePath)) {
            foreach (BitArray bitArray in bitArrays) {
                string line = $"{names[index]}: \n";
                Console.WriteLine($"{names[index]}: ");
                PrintBitArray(bitArray, names[index]);
                byte[] bytes = new byte[(bitArray.Length + 7) / 8];
                //if (index != 2) {
                //}
                bitArray.CopyTo(bytes, 0);
                byte[] reversedBytes = new byte[bytes.Length];
                for (int i = 0; i < reversedBytes.Length; i++) {
                    reversedBytes[i] = Mirror8Bits(bytes[i]);
                }
                foreach (byte b in reversedBytes) {
                    byte byteToSave = b;
                    char c = (char)byteToSave;
                    Console.Write($"{byteToSave} : 0x{byteToSave:X2} : {c} : {Convert.ToString(byteToSave, 2).PadLeft(8, '0')}");
                    line += $"{c}";
                    Console.WriteLine("");
                }
                //line += "\n=============";
                writer.WriteLine(line);
                index++;
            }
        }

        //File.WriteAllBytes(filePath, bytesToSave);
        Console.WriteLine($"\nРезультат сохранен в файл '{filePath}'");
    }

    private static void PrintBitArray(BitArray bitArray, string message) {
        Console.WriteLine($"========{message}========");
        foreach (bool bit in bitArray) {
            Console.Write(bit ? "1" : "0");
        }
        Console.WriteLine("");
    }
    private static void PrintMatrix(List<BitArray> matrix, string message) {
        Console.WriteLine($"========{message}========");
        foreach (BitArray bitArray in matrix) {
            foreach (bool bit in bitArray) {
                Console.Write(bit ? "1" : "0");
            }
            Console.WriteLine("");
        }
    }
    private static List<BitArray> GetBitMatrix(byte[] bytes) {
        List<BitArray> bitMatrices = new List<BitArray>();
        byte[] reversedBytes = new byte[bytes.Length];
        for (int i = 0; i < reversedBytes.Length; i++) {
            reversedBytes[i] = Mirror8Bits(bytes[i]);
        }
        foreach (byte b in reversedBytes) {
            BitArray bitArray = new BitArray(new byte[] { b });
            bitMatrices.Add(bitArray);
        }
        return bitMatrices;
    }
    private static byte[] ConvertToCorrectEncoding(byte[] initialBytes, int encodingCodePage, int finalCodePage) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding initialEncoding = Encoding.GetEncoding(encodingCodePage);
        string decodedString = initialEncoding.GetString(initialBytes);
        Encoding finalEncoding = Encoding.GetEncoding(finalCodePage);
        byte[] convertedBytes = finalEncoding.GetBytes(decodedString);
        return convertedBytes;
    }

    private static List<BitArray> ParityControl(List<BitArray> bitMatrix) {
        //контроль по паритету
        List<BitArray> resultBytes = new List<BitArray>();
        VControlSumBits = GetControlSumVertical(bitMatrix);
        for (int i = 0; i < bitMatrix.Count; i++) {
            BitArray resultByte = new BitArray(bitMatrix[i].Length + 1);
            for (int j = 0; j < bitMatrix[i].Length; j++) {
                resultByte[j] = bitMatrix[i][j];
            }
            resultByte[resultByte.Length - 1] = VControlSumBits[i];
            resultBytes.Add(resultByte);
        }
        return resultBytes;
    }
    private static BitArray GetControlSumVertical(List<BitArray> bitMatrix) {
        BitArray controlSumBits = new(bitMatrix.Count);
        //Проходим по каждому байту
        for (int i = 0; i < bitMatrix.Count; i++) {
            int count = 0;
            //Проходим по каждому биту
            foreach (bool bit in bitMatrix[i]) {
                //Если бит равен 1, то увеличиваем счетчик
                if (bit) {
                    count++;
                }
            }

            //Устанавливаем контрольный бит
            if (count % 2 == 1) {
                controlSumBits.Set(i, true);
            }
            else {
                controlSumBits.Set(i, false);
            }
        }
        return controlSumBits;
    }
    private static List<BitArray> VerticalAndHorizontalParityControl(List<BitArray> bitMatrix) {
        List<BitArray> resultBytes = new List<BitArray>();
        VControlSumBits = GetControlSumVertical(bitMatrix);
        HControlSumBits = GetControlSumHorizontal(bitMatrix);
        for (int i = 0; i < bitMatrix.Count; i++) {
            BitArray resultByte = new BitArray(bitMatrix[i].Length + 1);
            for (int j = 0; j < bitMatrix[i].Length; j++) {
                resultByte[j] = bitMatrix[i][j];
            }
            resultByte[resultByte.Length - 1] = VControlSumBits[i];
            resultBytes.Add(resultByte);
        }
        resultBytes.Add(HControlSumBits);
        return resultBytes;
    }
    private static BitArray GetControlSumHorizontal(List<BitArray> bitMatrix) {
        BitArray controlSumBits = new(bitMatrix[0].Count);
        int numRows = bitMatrix.Count;
        int numCols = bitMatrix[0].Count;
        for (int col = 0; col < numCols; col++) {
            int count = 0;
            for (int row = 0; row < numRows; row++) {
                if (bitMatrix[row][col])
                    count++;
            }
            if (count % 2 == 1) {
                controlSumBits.Set(col, true);
            }
            else {
                controlSumBits.Set(col, false);
            }
        }
        return controlSumBits;
    }
    static void PrintBinary(ushort value) {
        string binaryValue = "";
        for (int i = 15; i >= 0; i--) {
            binaryValue += ((value >> i) & 1) == 1 ? "1" : "0";
        }
        Console.WriteLine($"{binaryValue}");  // Output: 00010011}
    }
    static int CountFirstZeros(ushort number) {
        int numZeros = 0;

        // Count the number of zeros before the first one
        for (ushort mask = 0x8000; mask > 0; mask >>= 1) {
            if ((number & mask) == 0) {
                numZeros++;
            }
            else {
                break;
            }
        }

        return numZeros;
    }


    static byte Mirror8Bits(byte bytesToMirror) {
        byte mirrored = 0;
        for (int i = 0; i < 8; i++) {
            if ((bytesToMirror & (1 << i)) != 0) {
                mirrored |= (byte)(1 << (7 - i));
            }
        }
        return mirrored;
    }
    public static ushort MirrorBytesBySize(ushort value, int bitWidth) {
        ushort mirrored = 0;
        for (int i = 0; i < bitWidth; i++) {
            if ((value & (1 << i)) != 0) {
                mirrored |= (ushort)(1 << (bitWidth - 1 - i));
            }
        }
        return mirrored;
    }

    static ushort CalculateCRC(byte[] bytes) {
        ushort poly = 0x0805;           // setup example poly
        ushort crcsize = 14;           // setup example crc width
        ushort initialValue = 0;       // setup example initial value
        bool inputReflected = true;   // setup example input reflection
        bool resultReflected = true;  // setup example result reflection
        ushort finalXor = 0;           // setup example final XOR value

        ushort bitsToShift = (ushort)(crcsize - 8);       // example CRC-11: 3
        ushort bitmask = (ushort)(1 << (crcsize - 1));    // example CRC-11: 0x0400
        ushort finalmask = (ushort)((1 << crcsize) - 1);  // example CRC-11: 0x07FF

        ushort crc = initialValue;

        foreach (byte b in bytes) {
            byte curByte = (inputReflected ? Mirror8Bits(b) : b); ;
            crc ^= (ushort)(curByte << bitsToShift); /* move byte into MSB of CRC */

            for (int i = 0; i < 8; i++) {
                if ((crc & bitmask) != 0) {
                    crc = (ushort)(((crc << 1) ^ poly));
                }
                else {
                    crc <<= 1;
                }
            }

            crc = (ushort)(crc & finalmask);
        }

        crc = (ushort)(resultReflected ? MirrorBytesBySize(crc, crcsize) : crc);
        crc ^= finalXor;

        PrintBinary(crc);
        return (ushort)(crc);
    }
}