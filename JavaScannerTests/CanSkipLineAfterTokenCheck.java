import java.io.*;
import java.util.Scanner;

public class CanSkipLineAfterTokenCheck {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("First Line, second statement,\r\nSecond Line, fourth statement\r\n");
            s.useDelimiter(",\\s*");
            
            System.out.println(s.hasNextInt());
            System.out.println("\"" + s.next() + "\"");
            System.out.println(s.hasNextInt());
            System.out.println("\"" + s.nextLine() + "\"");
            System.out.println("\"" + s.next() + "\"");
            System.out.println("\"" + s.nextLine() + "\"");
            System.out.println("\"" + s.nextLine() + "\"");
        } finally {
            if (s != null) {
                s.close();
            }
        }
    }
}