import java.io.*;
import java.util.Scanner;

public class CanReturnEmptyStrings {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("string with  extra spaces ");

            while (s.hasNext()) {
                System.out.println("\"" + s.next() + "\"");
            }
        } finally {
            if (s != null) {
                s.close();
            }
        }
    }
}