import java.io.*;
import java.util.Scanner;

public class ScanLines {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("First line\r\n4453\r\nLast line");

						System.out.println(s.toString());
            System.out.println(s.hasNextLine());
						System.out.println(s.toString());
            System.out.println(s.nextLine());
						System.out.println(s.toString());
            System.out.println(s.hasNextLine());
            System.out.println(s.nextDouble());
						System.out.println(s.toString());
            System.out.println(s.hasNextLine());
            System.out.println(s.nextLine());
						System.out.println(s.toString());
            System.out.println(s.hasNextLine());
            System.out.println(s.nextLine());
            System.out.println(s.hasNextLine());
            System.out.println(s.nextLine());
        } finally {
            if (s != null) {
                s.close();
            }
        }
    }
}