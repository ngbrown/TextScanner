import java.io.*;
import java.util.Scanner;

public class ScanXanToArray {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("string with  extra spaces ");
            System.out.println("s = new Scanner(\"string with  extra spaces \");");
            System.out.println(s.toString());
            
            s.useDelimiter("\\s");
            System.out.println("s.useDelimiter(\"\\\\s\");");
            System.out.println(s.toString());
            
            while (s.hasNext()) {
                System.out.println("s.next() = \"" + s.next() + "\"");
            		System.out.println(s.toString());
            }
        } finally {
            if (s != null) {
                s.close();
            		System.out.println("s.close();");
            		System.out.println(s.toString());
            }
        }
    }
}