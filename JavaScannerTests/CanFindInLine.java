import java.io.*;
import java.util.Scanner;

public class CanFindInLine {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("1 fish 2 fish red fish blue fish");

            System.out.println("\"" + s.findInLine("(\\d+) fish (\\d+) fish (\\w+) fish (\\w+)") + "\"");
            System.out.println("\"" + s.next() + "\"");
            System.out.println("\"" + s.next() + "\"");
        } finally {
            if (s != null) {
                s.close();
            }
        }
    }
}