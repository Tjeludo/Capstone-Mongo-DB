# Update Linux packages
sudo apt update
sudo apt upgrade

# Install Python 3 and pip
sudo apt install python3 python3-pip

# Verify installation
python3 --version
pip3 --version

# Install virtual environment
sudo apt install python3-venv

# Create venv
python3 -m venv venv

# Activate
source venv/bin/activate  # Linux/macOS

# Deactivate
deactivate

# Install dependencies
pip3 install -r requirements.txt
# or
python3 -m pip install package-name
python3 -m pip install -r requirements.txt

# Get all dependencies
pip3 freeze > requirements.txt  

# Set user info (first time only)
git config --global user.name "Your Name"
git config --global user.email "you@example.com"

# Check configuration
git config --list
git config --global --edit   # opens config file

# Clone a repo
git clone git@github.com:username/repo.git

# List local branches
git branch

# List all branches including remote
git branch -a

# Switch to an existing local branch
git checkout branch-name
# or
git switch branch-name

# Create new local branch
git checkout -b new-branch

# Delete local branch
git branch -d branch-name

# Stage all changes
git add .

# Stage specific file(s)
git add file.py
# or
git add path/to/file
# or
git add *.png

# Commit changes
git commit -m "Describe your changes clearly"

# Check status before commit
git status

# Push current branch to its linked remote branch
git push


# Push specific branch to remote
git push origin branch-name

# Pull changes from the remote branch to your local branch tracks
git pull

# Pull specific branch
git pull origin branch-name

# Fetch updates from remote (does not merge)
git fetch

# Merge changes fetched from the remote branch into your current local branch
git merge origin/branch-name   