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
    static private ushort crc;
    static private string filePath = "";
    static private readonly byte[] testInitialBytes1 = [49, 50, 51, 52, 53, 54, 55, 56, 57];
    static private readonly ushort testResultBytes1 = 0x082D;
    static private readonly byte[] testInitialBytes2 = [57, 56, 55, 54, 53, 52, 51, 50, 49];
    static private readonly ushort testResultBytes2 = 0x1F74;
    static void Main() {
        //string filePath = "file1.txt";
        if (Test()) {
            Console.WriteLine("Тесты CRC прошли успешно.");
            filePath = GetExistingFile();
            byte[] incomingMessage = ReadFile();
            bitMatrix = GetBitMatrix((byte[])incomingMessage.Clone());
            parityControlMatrix = ParityControl();
            verticalAndHorizontalParityControlMatrix = VerticalAndHorizontalParityControl();
            crc = CalculateCRC((byte[])incomingMessage.Clone());
            //crcBits = CRC(newBytes);
            //PrintBinary(crc);
            PrintBytes((byte[])incomingMessage.Clone(), "Исходные данные");
            PrintMatrix(parityControlMatrix, "Контроль по паритету");
            PrintMatrix(verticalAndHorizontalParityControlMatrix, "Вертикальный и горизонтальный контроль по паритету");
            Console.WriteLine($"========CRC========");
            Console.WriteLine($"CRC: 0x{crc:X4}");
            Console.WriteLine($"============================");
            Console.WriteLine("");

            SaveToFile();
        }
        else {
            Console.WriteLine("Тесты CRC не пройдены. Попробуйте снова.");
        }
        //SaveToFile(newBytes, filePath);
    }
    private static bool Test() {
        bool testResult = true;
        if (CalculateCRC(testInitialBytes1) != testResultBytes1) {
            PrintBinary(CalculateCRC(testInitialBytes1));
            PrintBinary(testResultBytes1);
            Console.WriteLine("Тест 1 не пройден.");
            testResult = false;
        }
        if (CalculateCRC(testInitialBytes2) != testResultBytes2) {
            PrintBinary(CalculateCRC(testInitialBytes2));
            PrintBinary(testResultBytes2);
            Console.WriteLine("Тест 2 не пройден.");
            testResult = false;
        }

        return testResult;
    } 
    private static string GetExistingFile() {
        while (true) {
            Console.Write("Введите путь к файлу: ");
            string fileName = Console.ReadLine();

            if (File.Exists(fileName)) {
                //Console.WriteLine($"File '{fileName}' exists.");
                return fileName;
            }
            else {
                Console.WriteLine($"Файл '{fileName}' не существует. Попробуйте снова.");
            }
        }
    }
    //private static byte[] ReadFile() {
    //    byte[] bytes = new byte[new FileInfo(filePath).Length];
    //    int iter = 0;
    //    using (var stream = File.Open(filePath, FileMode.Open)) {
    //        using (var reader = new BinaryReader(stream)) {
    //            while (reader.PeekChar() > -1) {
    //                bytes[iter] = reader.ReadByte();
    //                iter++;
    //            }
    //        }
    //    }
    //    //byte[] fileBytes = File.ReadAllBytes(filePath);
    //    return bytes;
    //}
    private static byte[] ReadFile() {
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
            byte[] buffer = new byte[1024]; // Use a reasonable buffer size
            using (var memoryStream = new MemoryStream()) {
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                return memoryStream.ToArray();
            }
        }
    }
    private static void SaveToFile() {
        filePath = filePath.Substring(0, filePath.Length - 4) + ".ccs";
        Console.WriteLine("Сохраняются байты: ");
        List<BitArray> bitArrays = new List<BitArray>() { VControlSumBits, HControlSumBits };
        List<String> names = ["Вертикальная контрольная сумма", "Горизонтальная контрольная сумма", "CRC"];
        int index = 0;
        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8)) {
            foreach (BitArray bitArray in bitArrays) {
                string line = $"{names[index]}: \n";
                byte[] bytes = new byte[(bitArray.Length + 7) / 8];
                //if (index != 2) {
                //}
                bitArray.CopyTo(bytes, 0);
                byte[] reversedBytes = new byte[bytes.Length];
                for (int i = 0; i < reversedBytes.Length; i++) {
                    reversedBytes[i] = Mirror8Bits(bytes[i]);
                }
                PrintBytes(reversedBytes, names[index]);
                foreach (byte b in reversedBytes) {
                    byte byteToSave = b;
                    char c = (char)byteToSave;
                    line += $"{c} -> {byteToSave}\n";
                }
                writer.WriteLine(line);
                index++;
            }
            byte[] crcBytes = BitConverter.GetBytes(crc);
            string crcLine = $"{names[index]}: \n";
            Array.Reverse(crcBytes);
            PrintBytes(crcBytes, names[index]);
            foreach (byte byteToSave in crcBytes) {
                char c = (char)byteToSave;
                crcLine += $"{c} -> {byteToSave}\n";
            }
            writer.WriteLine(crcLine);
        }

        //File.WriteAllBytes(filePath, bytesToSave);
        Console.WriteLine($"\nРезультат сохранен в файл '{filePath}'");
    }

    private static void PrintMatrix(List<BitArray> matrix, string message) {
        Console.WriteLine($"========{message}========");
        foreach (BitArray bitArray in matrix) {
        Console.Write("0b");
            foreach (bool bit in bitArray) {
                Console.Write(bit ? "1" : "0");
            }
            Console.WriteLine("");
        }
    }
    private static void PrintBytes(byte[] bytes, string message) {
        Console.WriteLine($"========{message}========");
        foreach (byte b in bytes) {
            Console.WriteLine($"char: {(char)b} -> {b} -> 0x{b:X4} -> 0b{Convert.ToString(b, 2)}");
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
    private static List<BitArray> ParityControl() {
        //контроль по паритету
        List<BitArray> resultBytes = new List<BitArray>();
        VControlSumBits = GetControlSumVertical();
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
    private static BitArray GetControlSumVertical() {
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
    private static List<BitArray> VerticalAndHorizontalParityControl() {
        List<BitArray> resultBytes = new List<BitArray>();
        VControlSumBits = GetControlSumVertical();
        HControlSumBits = GetControlSumHorizontal();
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
    private static BitArray GetControlSumHorizontal() {
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
        Console.WriteLine($"{binaryValue}"); 
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
    public static ushort MirrorBitsBySize(ushort value, int bitWidth) {
        ushort mirrored = 0;
        for (int i = 0; i < bitWidth; i++) {
            if ((value & (1 << i)) != 0) {
                mirrored |= (ushort)(1 << (bitWidth - 1 - i));
            }
        }
        return mirrored;
    }

    static ushort CalculateCRC(byte[] bytes) {
        ushort poly = 0x0805;         
        ushort crcsize = 14;          
        ushort initialValue = 0;      
        bool inputReflected = true;   
        bool resultReflected = true;  
        ushort finalXor = 0;           

        ushort bitsToShift = (ushort)(crcsize - 8);       
        ushort bitmask = (ushort)(1 << (crcsize - 1));    
        ushort finalmask = (ushort)((1 << crcsize) - 1);  

        crc = initialValue;

        foreach (byte b in bytes) {
            byte currByte = (inputReflected ? Mirror8Bits(b) : b);
            //Console.WriteLine($"processing byte: {b :X2}");
            crc ^= (ushort)(currByte << bitsToShift);
            for (int i = 0; i < 8; i++) {
            //PrintBinary(crc);
                
                if ((crc & bitmask) != 0) {
                    //Console.WriteLine("XOR: ");
                    //PrintBinary(poly);
                    crc = (ushort)(((crc << 1) ^ poly));
                }
                else {
                    crc <<= 1;
                }
                //Console.WriteLine("");
            }

            crc = (ushort)(crc & finalmask);
        }

        crc = (ushort)(resultReflected ? MirrorBitsBySize(crc, crcsize) : crc);
        crc ^= finalXor;

        //PrintBinary(crc);
        return (ushort)(crc);
    }
}