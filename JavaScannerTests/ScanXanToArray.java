import java.io.*;
import java.util.Scanner;

public class ScanXanToArray {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("string with  extra spaces ");
            s.useDelimiter("\\s");
            
            System.out.println("{");
            while (s.hasNext()) {
                System.out.println("@\"" + s.next() + "\",");
            }
            System.out.println("};");
        } finally {
            if (s != null) {
                s.close();
            }
        }
    }
}