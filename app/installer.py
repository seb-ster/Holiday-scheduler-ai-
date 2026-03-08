import time
import sys

def display_splash_screen():
    print("Welcome to the Installation")
    print("Launching installer...")
    time.sleep(2)  # Simulating splash display time

def display_progress_bar(total):
    for i in range(total + 1):
        percent = (i / total) * 100
        remaining_time = (total - i) * 0.1  # Assuming each step takes approximately 0.1 seconds
        sys.stdout.write(f"\rProgress: [{'\#' * i}{'.' * (total - i)}] {percent:.2f}% | Remaining time: {remaining_time:.1f}s")
        sys.stdout.flush()
        time.sleep(0.1)  # Simulated time for installation step

def main():
    display_splash_screen()
    total_steps = 100
    display_progress_bar(total_steps)
    print("\nInstallation complete!")

if __name__ == "__main__":
    main()