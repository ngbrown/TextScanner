import java.io.*;
import java.util.Scanner;

public class CanFindInLineToEnd {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("\r\n1 fish 2 fish red fish blue fish");

            System.out.println("\"" + s.findInLine("^(([^\\s]+) fish\\s*)+$") + "\"");
            System.out.println(s.match().toString());
        } finally {
            if (s != null) {
                s.close();
            }
        }
    }
}