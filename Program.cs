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

    private static void CRC() {
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