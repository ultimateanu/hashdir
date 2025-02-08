FROM fedora:41

RUN dnf install -y \
    dotnet-sdk-8.0 \
    make \
    findutils \
    tmux \
    htop \
    wget && \
    dnf clean all

RUN dotnet tool install --global fantomas --version 7.0.0

WORKDIR /mnt/code

CMD ["/bin/bash"]
