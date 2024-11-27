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
    static private BitArray crcBits = new BitArray(0);
    static private BitArray VControlSumBits = new BitArray(0);
    static private BitArray HControlSumBits = new BitArray(0);
    static private BitArray resultXORBits = new BitArray(new byte[] { 0x00, 0x00 });
    static void Main() {
        string filePath = "file.txt";
        byte[] bytes = ReadFile(filePath);
        //foreach (byte b in bytes) {
        //    Console.WriteLine($"{b :X2} : {b}");
        //}
        foreach(byte b in bytes) {
            Console.WriteLine($"0x{b:X2} : {b}");
        }
        byte[] newBytes = ConvertToCorrectEncoding(bytes, 1251, 866);
        Console.WriteLine("=1===");
        foreach (byte b in newBytes) {
            Console.WriteLine($"0x{b:X2} : {b}");
        }
        Console.WriteLine("==1==");
        bitMatrix = GetBitMatrix(newBytes);

        parityControlMatrix = ParityControl(bitMatrix);
        verticalAndHorizontalParityControlMatrix = VerticalAndHorizontalParityControl(bitMatrix);
        crcBits = CRC(newBytes);

        PrintMatrix(bitMatrix, "Исходные данные");
        PrintMatrix(parityControlMatrix, "Контроль по паритету");
        PrintMatrix(verticalAndHorizontalParityControlMatrix, "Вертикальный и горизонтальный контроль по паритету");
        PrintBitArray(crcBits, "CRC");
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
        List<BitArray> bitArrays = new List<BitArray>() { VControlSumBits, HControlSumBits, crcBits };
        List<String> names = ["Вертикальная контрольная сумма", "Горизонтальная контрольная сумма", "Контрольная сумма CRC"];
        int index = 0;
        using (StreamWriter writer = new StreamWriter(filePath)) {
            foreach (BitArray bitArray in bitArrays) {
                string line = $"{names[index]}: \n";
                Console.WriteLine($"{names[index]}: ");
                byte[] bytes = new byte[(bitArray.Length + 7) / 8];
                bitArray.CopyTo(bytes, 0);
                foreach (byte b in bytes) {
                    byte byteToSave = b;
                    if (index != 2) {
                        byteToSave = ReverseBits(b);
                    }
                    char c = (char)byteToSave;
                    Console.Write($"{byteToSave} : 0x{byteToSave:X2} : {c} ");
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
    private static BitArray resultXOR(BitArray bitArray1) {
        BitArray result = bitArray1.Xor(resultXORBits);
        return result;
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

        for (int i = 0; i < bytes.Length; i++) {
            reversedBytes[i] = ReverseBits(bytes[i]);
            //Console.WriteLine(reversedBytes[i]);
        }
        foreach (byte b in reversedBytes) {
            BitArray bitArray = new BitArray(new byte[] { b });
            bitMatrices.Add(bitArray);
        }
        return bitMatrices;
    }
    public static byte ReverseBits(byte b) {
        byte result = 0;
        for (int i = 0; i < 8; i++) {
            // Сдвигаем результат влево на один бит
            result <<= 1;
            // Устанавливаем младший бит результата равным текущему биту входного байта
            result |= (byte)(b & 1);
            // Сдвигаем входной байт вправо на один бит
            b >>= 1;
        }
        return result;
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
    private static BitArray AddFrame(BitArray initialBitArray, BitArray polynomial) {
        int frameLength = 0;
        int ind = 0;
        do {
            //Console.WriteLine($"{ind}: {polynomial[ind]} = {polynomial.Length - ind}");
            frameLength = polynomial.Length - ind - 2;
            ind++;
        } while (!polynomial[ind]);
        //Console.WriteLine($"Кадр: {frameLength}");
        BitArray newBitArray = new BitArray(initialBitArray.Length + frameLength);
        for (int i = 0; i < initialBitArray.Length; i++) {
            newBitArray[i] = initialBitArray[i];
        }
        for (int i = initialBitArray.Length; i < newBitArray.Length; i++) {
            newBitArray[i] = false;
        }
        return newBitArray;
    }
    private static BitArray TrimBitArray(BitArray bitArray) {
        // Find the index of the first true bit
        int firstTrueIndex = -1;
        for (int i = 0; i < bitArray.Length; i++) {
            if (bitArray[i]) {
                firstTrueIndex = i;
                break;
            }
        }

        // Handle the case where all bits are false
        if (firstTrueIndex == -1) {
            return new BitArray(0); // Return an empty BitArray
        }

        // Create a new BitArray from the first true bit to the end
        int newLength = bitArray.Length - firstTrueIndex;
        BitArray trimmedBitArray = new BitArray(newLength);
        for (int i = 0; i < newLength; i++) {
            trimmedBitArray[i] = bitArray[firstTrueIndex + i];
        }

        return trimmedBitArray;
    }
    private static BitArray CRC(byte[] bytes) {
        //byte[] newbytes = bytes;
        //BitArray polynomial = new BitArray(new byte[] { 0x08, 0x05 });
        byte[] poly2 = new byte[] { ReverseBits(0x08), ReverseBits(0x05) };
        foreach (byte b in poly2) {
            Console.WriteLine($"{b} : {b :X2}");
        }
        BitArray polynomial = new BitArray(poly2);
        BitArray num1 = new BitArray(bytes);
        PrintBitArray(num1, "num1");
        for (int i = 0; i < bytes.Length; i++) {
            bytes[i] = ReverseBits(bytes[i]);
        }
        BitArray num2 = new BitArray(bytes);
        PrintBitArray(num2, "num2");
        BitArray num = AddFrame(num1, polynomial);
        polynomial = TrimBitArray(new BitArray(poly2));
        Console.WriteLine("+++");
        foreach (bool bit in polynomial) {
            Console.Write(bit ? "1" : "0");
        }
        Console.WriteLine("\n+++");
        BitArray zero = new BitArray(new byte[] { 0x00, 0x00 });
        BitArray initialNum = new BitArray(num);

        Console.WriteLine("========CRC========");
        foreach (bool bit in num) {
            Console.Write(bit ? "1" : "0");
        }
        Console.WriteLine("");
        Console.WriteLine("XOR");
        foreach (bool bit in polynomial) {
            Console.Write(bit ? "1" : "0");
        }
        Console.WriteLine("");

        int index = 0;
        Console.WriteLine($"poly len: {polynomial.Length}; num len {num.Length}; Limit {num.Length - polynomial.Length}");
        while (index + polynomial.Length <= num.Length) {
            Console.WriteLine($"NEW {index} + {polynomial.Length} < {num.Length}");
            Console.WriteLine("===");
            for (int i = 0; i < polynomial.Length; i++) {
                Console.Write(num[i + index] ? "1" : "0");
            }
            Console.WriteLine("");
            for (int i = 0; i < polynomial.Length; i++) {
                if (num[0 + index]) {
                    Console.Write(polynomial[i] ? "1" : "0");
                }
                else {
                    Console.Write("0");
                }
            }
            Console.WriteLine("");
            Console.WriteLine("===");
            List<bool> buffer = new List<bool>();
            for (int i = 0; i < polynomial.Length; i++) {
                bool temp = false;
                if (!num[0 + index]) {
                    Console.Write($"{i + index}: [{0 + index}]={num[0 + index]} -> ");
                    Console.Write($"div [${i + index}] by zero: {num[i + index]} XOR {zero[i]} = ");
                    temp = num[i + index] ^ zero[i];
                    
                    //num[i + index] = num[i + index] ^ zero[i];
                    
                }
                else {
                    Console.Write($"{i + index}: [{0 + index}]={num[0 + index]} -> ");
                    Console.Write($"div [${i + index}] by polynomial[${i}]: {num[i + index]} XOR {polynomial[i]} = ");
                    temp = num[i + index] ^ polynomial[i];
                    //num[i + index] = num[i + index] ^ polynomial[i];
                }
                    buffer.Add(temp);
                    Console.WriteLine(temp ? "1" : "0");
            
            }
            for (int i = 0; i < polynomial.Length; i++) {
                
                num[i + index] = buffer[i];
            }
            Console.Write("result: ");
            //for (int i = 0; i < polynomial.Length; i++) {
            //  Console.Write(num[i + index + 1] ? "1" : "0");
            //}
            
            index++;
            //foreach (bool bit in num) {
            //    Console.Write(bit ? "1" : "0");
            //}
            Console.WriteLine("");
        }
        
        //BitArray result = new BitArray(num.Length);
        BitArray result = new BitArray(polynomial.Length);
        //Console.WriteLine("========CRC========");
        //for (int i = 0; i < num.Length - polynomial.Length; i++) { 
        //    result[i] = initialNum[i];
        //}
        for (int i = num.Length - polynomial.Length; i < num.Length; i++) {
            result[i - (num.Length - polynomial.Length)] = num[i];
        }

        //foreach (bool bit in result) {
        //    Console.Write(bit ? "1" : "0");
        //}
        //Console.WriteLine("");
        //result = resultXOR(result);
        return result;
    }
}