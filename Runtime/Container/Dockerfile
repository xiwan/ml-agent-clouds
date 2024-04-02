FROM nvidia/cuda:12.3.1-runtime-ubuntu22.04

ENV DEBIAN_FRONTEND=noninteractive \
    TZ=America/Los_Angeles

# Install dependencies
RUN apt-get update && apt-get install -y \
    git \
    make build-essential libssl-dev zlib1g-dev \
    libbz2-dev libreadline-dev libsqlite3-dev wget curl llvm \
    libncursesw5-dev xz-utils tk-dev libxml2-dev libxmlsec1-dev libffi-dev liblzma-dev git git-lfs  \
    ffmpeg libsm6 libxext6 cmake libgl1-mesa-glx \
    && rm -rf /var/lib/apt/lists/* \
    && git lfs install

# Create and switch to a new user
RUN useradd -m -u 1000 user
USER user
ENV HOME=/home/user \
    PATH=/home/user/.local/bin:$PATH

# Pyenv and Python setup
RUN curl https://pyenv.run | bash
ENV PATH=$HOME/.pyenv/shims:$HOME/.pyenv/bin:$PATH
ARG PYTHON_VERSION=3.9.18
RUN pyenv install $PYTHON_VERSION && \
    pyenv global $PYTHON_VERSION && \
    pyenv rehash && \
    pip install --no-cache-dir --upgrade pip setuptools wheel 

# Set the working directory
RUN mkdir /home/user/Mlagents
WORKDIR /home/user/Mlagents

# Clone Mlagents directly into /home/user/Mlagents
RUN git clone --branch release_20 --recursive https://github.com/Unity-Technologies/ml-agents.git .

# Create a Python virtual environment in a directory
ENV TEMP_VENV_PATH=/home/user/Mlagents/.venv
RUN python -m venv $TEMP_VENV_PATH

RUN . $TEMP_VENV_PATH/bin/activate 
RUN pip install torch -f https://download.pytorch.org/whl/torch_stable.html 
RUN pip install -e ./ml-agents-envs 
RUN pip install -e ./ml-agents

# Downgrade protobuf
RUN pip install protobuf==3.20.*