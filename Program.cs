using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

class Program {
    static void Main() {
        string filePath = "file.txt";
        byte[] bytes = ReadFile(filePath);
        byte[] newBytes = ConvertToCorrectEncoding(bytes, 1251, 866);
        List<BitArray> bitMatrix = GetBitMatrix(newBytes);
        Console.WriteLine("================");
        //НЕ ЗАБЫТЬ УБРАТЬ
        foreach (byte b in newBytes) {
            Console.WriteLine($"{b:X2} : {ByteToBinary(b)} :");
            //Console.WriteLine(bitArray.ToString());

        }
        PrintMatrix(bitMatrix, "Исходные данные");
        List<BitArray> parityControlBits = ParityControl(bitMatrix);
        List<BitArray> verticalAndHorizontalParityControlBits = VerticalAndHorizontalParityControl(bitMatrix);
        PrintMatrix(parityControlBits, "Контроль по паритету");
        PrintMatrix(verticalAndHorizontalParityControlBits, "Вертикальный и горизонтальный контроль по паритету");
        CRC(newBytes);
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
        foreach (byte b in bytes) {
            BitArray bitArray = new BitArray(new byte[] { b });
            bitMatrices.Add(bitArray);
        }
        return bitMatrices;
    }
    private static byte[] ConvertToCorrectEncoding(byte[] initialBytes, int encodingCodePage, int finalCodePage) {
        Encoding initialEncoding = Encoding.GetEncoding(encodingCodePage);
        string decodedString = initialEncoding.GetString(initialBytes);
        Encoding finalEncoding = Encoding.GetEncoding(finalCodePage);
        byte[] convertedBytes = finalEncoding.GetBytes(decodedString);
        return convertedBytes;
    }
    private static byte[] ReadFile(string filePath) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] fileBytes = File.ReadAllBytes(filePath);
        return fileBytes;
    }
    //НЕ ЗАБЫТЬ УБРАТЬ
    static string ByteToBinary(byte b) {
        return Convert.ToString(b, 2).PadLeft(8, '0');
    }

    private static List<BitArray> ParityControl(List<BitArray> bitMatrix) {
        //контроль по паритету
        List<BitArray> resultBytes = new List<BitArray>();
        BitArray controlSumBits = GetControlSumVertical(bitMatrix);
        for (int i = 0; i < bitMatrix.Count; i++) { 
            BitArray resultByte = new BitArray(bitMatrix[i].Length + 1);
            for (int j = 0; j < bitMatrix[i].Length; j++) {
                resultByte[j] = bitMatrix[i][j];
            }
            resultByte[resultByte.Length - 1] = controlSumBits[i];
            resultBytes.Add(resultByte);
            
        }
        return resultBytes;
    }
    private static BitArray GetControlSumVertical(List<BitArray> bitMatrix) {
        BitArray controlSumBits = new(bitMatrix.Count);
        //Проходим по каждому байту
        for(int i = 0; i < bitMatrix.Count; i++) {
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
        BitArray controlSumBitsVertical = GetControlSumVertical(bitMatrix);
        BitArray controlSumBitsHorizontal = GetControlSumHorizontal(bitMatrix);
        for (int i = 0; i < bitMatrix.Count; i++) {
            BitArray resultByte = new BitArray(bitMatrix[i].Length + 1);
            for (int j = 0; j < bitMatrix[i].Length; j++) {
                resultByte[j] = bitMatrix[i][j];
            }
            resultByte[resultByte.Length - 1] = controlSumBitsVertical[i];
            resultBytes.Add(resultByte);
        }
        resultBytes.Add(controlSumBitsHorizontal);
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
            Console.WriteLine($"{ind}: {polynomial[ind]} = {polynomial.Length - ind}");
            frameLength = polynomial.Length - ind - 2;
            ind++;
        } while (!polynomial[ind]);
        Console.WriteLine($"Кадр: {frameLength}");
        BitArray newBitArray = new BitArray(initialBitArray.Length + frameLength);
        for (int i = 0; i < initialBitArray.Length; i++) {
            newBitArray[i] = initialBitArray[i];
        }
        for (int i = initialBitArray.Length; i < newBitArray.Length; i++) { 
            newBitArray[i] = false;
        }
        return newBitArray;
    }
    private static void CRC(byte[] bytes) {
        
        BitArray polynomial = new BitArray(new byte[] { 0x08, 0x05 });
        BitArray num = AddFrame(new BitArray(bytes), polynomial);
        BitArray zero = new BitArray(new byte[] { 0x00, 0x00 });
        //BitArray num = new BitArray(num.Length + polynomial.Length);
        Console.WriteLine("========CRC========");
        foreach (bool bit in polynomial) {
            Console.Write(bit ? "1 " : "0 ");
        }
        Console.WriteLine("");
        foreach (bool bit in num) {
            Console.Write(bit ? "1 " : "0 ");
        }
        Console.WriteLine("");

        int index = 0;
        Console.WriteLine($"poly len: {polynomial.Length}; num len {num.Length}; Limit {num.Length - polynomial.Length}");
        while (index <= num.Length - polynomial.Length) {
            Console.WriteLine($"NEW {index}");
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
            for (int i = 0; i < polynomial.Length; i++) {
                Console.Write($"{i + index}: {num[0 + index]} -> ");
                if (!num[0 + index]) {
                    Console.Write($"div [${i + index}] by zero: {num[i + index]} XOR {zero[i]} = ");
                    num[i + index] = num[i + index] ^ zero[i];
                    Console.WriteLine($"{num[i + index]}");
                }
                else {
                    Console.Write($"div [${i+index}] by polynomial[${i}]: {num[i + index]} XOR {polynomial[i]} = ");
                    num[i + index] = num[i + index] ^ polynomial[i];
                    Console.WriteLine($"{num[i + index]}");
                }
            }
            foreach (bool bit in num) {
                Console.Write(bit ? "1" : "0");
            }
            Console.WriteLine("");
            index++;
        }
        //  num - исходное число
        //  polynomial - образующий многочлен
        //  index = 0
        //  while(index != num.size - polynomial.size){
        //    for(int i = 0 + index; i < polynomial.size + index; i++){
        //        if(num[0] == 0)
        //            num[i] = num[i] XOR 0x0
        //        else
        //            num[i] = num[i] XOR polynomial[i]
        //    }
        //  }

        
    }
}