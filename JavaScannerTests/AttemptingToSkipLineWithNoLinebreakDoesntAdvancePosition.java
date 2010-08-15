import java.io.*;
import java.util.Scanner;

public class AttemptingToSkipLineWithNoLinebreakDoesntAdvancePosition {
    public static void main(String[] args) throws IOException {
        Scanner s = null;
        try {
            s = new Scanner("First line, second statement, third statement");
            s.useDelimiter(",\\s*");

						System.out.println(s.next());
            System.out.println(s.nextLine());
						System.out.println(s.next());
            System.out.println(s.hasNext());
        } finally {
            if (s != null) {
                s.close();
            }
        }
    }
}